#include "pico/stdlib.h"
#include "hardware/adc.h"
#include "hardware/gpio.h"
#include <stdio.h>
#include <string.h>

#define ODT_VERSION "0.1.0"
#define MAX_PINS 8

static int active_pins[MAX_PINS];
static size_t active_count = 0;

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
    gpio_init((uint)pin);
    gpio_set_dir((uint)pin, GPIO_IN);
    gpio_disable_pulls((uint)pin);
    active_pins[active_count++] = pin;
}

static void voltage_read(void) {
    adc_init();
    adc_gpio_init(26);
    adc_select_input(0);
    sleep_us(100);
    uint16_t raw = adc_read();
    double volts = ((double)raw * 3.3) / 4095.0;
    printf("VOLTAGE %.4f\n", volts);
}

static void sample_once(void) {
    printf("SAMPLE");
    for (size_t i = 0; i < active_count; ++i)
        printf(" %d=%d", active_pins[i], gpio_get((uint)active_pins[i]) ? 1 : 0);
    printf("\n");
}

static void handle_line(char *line) {
    if (!strcmp(line, "HELLO")) {
        printf("HELLO %s 1 SNIFF UART GPIO\n", ODT_VERSION);
    } else if (!strcmp(line, "SAFE")) {
        all_high_z();
        printf("OK SAFE\n");
    } else if (!strcmp(line, "VOLTAGE")) {
        voltage_read();
    } else if (!strncmp(line, "SNIFF ", 6)) {
        all_high_z();
        char *p = line + 6;
        while (*p && active_count < MAX_PINS) {
            int pin = 0;
            if (sscanf(p, "%d", &pin) == 1) add_input_pin(pin);
            while (*p && *p != ',') ++p;
            if (*p == ',') ++p;
        }
        printf("OK SNIFF %u\n", (unsigned)active_count);
    } else if (!strcmp(line, "SAMPLE")) {
        sample_once();
    } else {
        printf("ERR UNKNOWN_COMMAND\n");
    }
}

int main(void) {
    stdio_init_all();
    sleep_ms(1500);
    all_high_z();
    printf("ODT_READY %s\n", ODT_VERSION);

    char line[128];
    size_t n = 0;
    while (true) {
        int c = getchar_timeout_us(1000);
        if (c < 0) continue;
        if (c == '\r' || c == '\n') {
            if (n) { line[n] = '\0'; handle_line(line); n = 0; }
        } else if (n + 1 < sizeof(line)) {
            line[n++] = (char)c;
        } else {
            n = 0;
            printf("ERR LINE_TOO_LONG\n");
        }
    }
}
