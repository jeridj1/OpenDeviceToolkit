#include "hardware/pio.h"
#include "hardware/dma.h"
#include "pico/stdlib.h"

// PIO/DMA capture primitive. Higher-level decoding remains on the host.
static const uint16_t capture_program_instructions[] = {
    0x0001, // IN PINS, 1
    0x0000  // JMP 0
};

void odt_logic_capture(PIO pio, uint sm, uint pin, uint32_t *buffer, size_t words, uint sample_hz) {
    (void)capture_program_instructions;
    pio_gpio_init(pio, pin);
    pio_sm_set_consecutive_pindirs(pio, sm, pin, 1, false);
    pio_sm_set_enabled(pio, sm, false);
    pio_sm_clear_fifos(pio, sm);
    pio_sm_set_clkdiv(pio, sm, (float)clock_get_hz(clk_sys) / (float)sample_hz);
    // DMA transfer wiring is the next firmware step; this establishes the capture contract.
    (void)buffer;
    (void)words;
}
