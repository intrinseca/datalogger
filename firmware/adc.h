#ifndef ADC_H
#define ADC_H

void adc_init(void);
void adc_set_sampling_rate(unsigned int rate);
void adc_isr(void);
void adc_tick(void);
void * adc_get_filled_buff(void);
void adc_free_buff(void * buff);
#endif