#include "pico/stdlib.h"
#include "hardware/adc.h"
#include <stdio.h>
#include <string.h>
#include <stdint.h>

#define ODT_VERSION "0.3.0"
#define MAX_PINS 8
#define CAPTURE_WORDS 1024

static int active_pins[MAX_PINS];
static size_t active_count = 0;
static uint32_t capture_buffer[CAPTURE_WORDS];
extern bool odt_logic_capture(uint pin, uint sample_hz, uint32_t *buffer, size_t words);

static void all_high_z(void) {
    for (size_t i = 0; i < active_count; ++i) {
        gpio_set_dir((uint)active_pins[i], GPIO_IN);
        gpio_disable_pulls((uint)active_pins[i]);
    }
    active_count = 0;
}
static bool valid_gpio(int pin) { return pin >= 0 && pin <= 29; }
static void add_input_pin(int pin) {
    if (!valid_gpio(pin) || active_count >= MAX_PINS) return;
    gpio_init((uint)pin); gpio_set_dir((uint)pin, GPIO_IN); gpio_disable_pulls((uint)pin);
    active_pins[active_count++] = pin;
}
static void voltage_read(void) {
    adc_init(); adc_gpio_init(26); adc_select_input(0); sleep_us(100);
    uint16_t raw = adc_read(); printf("VOLTAGE %.4f\n", ((double)raw * 3.3) / 4095.0);
}
static void capture(uint pin, uint rate, uint words) {
    if (!valid_gpio((int)pin) || rate == 0 || words == 0 || words > CAPTURE_WORDS) { printf("ERR BAD_CAPTURE\n"); return; }
    all_high_z();
    if (!odt_logic_capture(pin, rate, capture_buffer, words)) { printf("ERR CAPTURE_FAILED\n"); return; }
    printf("CAPTURE %u %u", rate, words);
    for (uint i = 0; i < words; ++i) printf(" %08lx", (unsigned long)capture_buffer[i]);
    printf("\n");
}
static void handle_line(char *line) {
    if (!strcmp(line, "HELLO")) printf("HELLO %s 1 SNIFF VOLTAGE LOGIC_CAPTURE\n", ODT_VERSION);
    else if (!strcmp(line, "SAFE")) { all_high_z(); printf("OK SAFE\n"); }
    else if (!strcmp(line, "VOLTAGE")) voltage_read();
    else if (!strncmp(line, "SNIFF ", 6)) {
        all_high_z(); char *p = line + 6;
        while (*p && active_count < MAX_PINS) {
            int pin = -1; if (sscanf(p, "%d", &pin) == 1) add_input_pin(pin);
            while (*p && *p != ',') ++p; if (*p == ',') ++p;
        }
        printf("OK SNIFF %u\n", (unsigned)active_count);
    } else if (!strncmp(line, "CAPTURE ", 8)) {
        unsigned pin = 0, rate = 0, words = 0;
        if (sscanf(line + 8, "%u %u %u", &pin, &rate, &words) == 3) capture(pin, rate, words); else printf("ERR BAD_CAPTURE\n");
    } else printf("ERR UNKNOWN_COMMAND\n");
}
int main(void) {
    stdio_init_all(); sleep_ms(1500); all_high_z(); printf("ODT_READY %s\n", ODT_VERSION);
    char line[160]; size_t n = 0;
    while (true) {
        int c = getchar_timeout_us(1000); if (c < 0) continue;
        if (c == '\r' || c == '\n') { if (n) { line[n] = '\0'; handle_line(line); n = 0; } }
        else if (n + 1 < sizeof(line)) line[n++] = (char)c;
        else { n = 0; printf("ERR LINE_TOO_LONG\n"); }
    }
}
