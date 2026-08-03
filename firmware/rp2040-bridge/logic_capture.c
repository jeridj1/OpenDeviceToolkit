#include "hardware/pio.h"
#include "hardware/dma.h"
#include "pico/stdlib.h"
#include "logic_capture.pio.h"

static int dma_chan = -1;
static PIO capture_pio = pio0;
static uint capture_sm = 0;

bool odt_logic_capture(uint pin, uint sample_hz, uint32_t *buffer, size_t words) {
    if (pin > 29 || sample_hz == 0 || !buffer || words == 0) return false;
    uint offset = pio_add_program(capture_pio, &odt_capture_program);
    pio_sm_config c = odt_capture_program_get_default_config(offset);
    sm_config_set_in_pins(&c, pin);
    sm_config_set_in_shift(&c, false, true, 32);
    float div = (float)clock_get_hz(clk_sys) / (float)sample_hz;
    if (div < 1.0f) div = 1.0f;
    sm_config_set_clkdiv(&c, div);
    pio_gpio_init(capture_pio, pin);
    pio_sm_set_consecutive_pindirs(capture_pio, capture_sm, pin, 1, false);
    pio_sm_init(capture_pio, capture_sm, offset, &c);
    pio_sm_clear_fifos(capture_pio, capture_sm);
    if (dma_chan < 0) dma_chan = dma_claim_unused_channel(true);
    dma_channel_config dc = dma_channel_get_default_config(dma_chan);
    channel_config_set_transfer_data_size(&dc, DMA_SIZE_32);
    channel_config_set_read_increment(&dc, false);
    channel_config_set_write_increment(&dc, true);
    channel_config_set_dreq(&dc, pio_get_dreq(capture_pio, capture_sm, false));
    dma_channel_configure(dma_chan, &dc, buffer, &capture_pio->rxf[capture_sm], words, true);
    pio_sm_set_enabled(capture_pio, capture_sm, true);
    dma_channel_wait_for_finish_blocking(dma_chan);
    pio_sm_set_enabled(capture_pio, capture_sm, false);
    pio_remove_program(capture_pio, &odt_capture_program, offset);
    return true;
}
