/* ADC sampling code.
 *
 * This code is responsible for setup and sampling of the ADC, including the
 * use of a timer module for accurate timing.
 */

#include <p18f4550.h>
#include "adc.h"

#include "HardwareProfile.h"

#define ADC_BUFF_SIZE 64

typedef struct adc_buffer_type {
    unsigned char * buff;
    unsigned short size;
    unsigned short pos;
} adc_buffer_t;

/*
 * Setup ADC and timer hardware.
 */
void adc_init(void)
{
    PIE1bits.ADIE = 0;  // ensure interrupts disabled
    PIR1bits.ADIF = 0;  // clear any pending interrupts

    ADCON0bits.ADON = 0; // turn off the ADC
    
    /* configure ADC hardware */
    ADCON0 &= ~(0xF << 2); // select analog channel 1
    ADCON0 |= 1 << 2;

    ADCON1bits.VCFG1 = 0; // use internal voltage references
    ADCON1bits.VCFG0 = 0;

    ADCON1 &= ~(0x0F);  // enable AN0+AN1 only as analog channels
    ADCON1 |= 0x0D;

    ADCON2bits.ADFM = 1; // right justify sample data

    ADCON2 &= ~(0x3F);
    ADCON2 |= 0x02 << 3; // Enable 4Tad acquisition time
    ADCON2 |= 0x06; // enable Fosc/64 conversion clock

    ADCON0bits.ADON = 1; // turn on the ADC

    /* configure timer 1 with CCP2 for sampling rate */
    PIE1bits.TMR1IE = 0; // ensure interrupt is disabled

    T1CONbits.RD16 = 1; // 16 bit read/writes
    T1CONbits.T1CKPS1 = 0;
    T1CONbits.T1CKPS0 = 0; // 1x prescaling
    T1CONbits.T1OSCEN = 0; // disable Timer 1 osc.
    T1CONbits.NOT_T1SYNC = 0; // synchronise to external clock
    T1CONbits.TMR1CS = 0; // use internal clock

    TMR1H = 0;
    TMR1L = 0;

    /* configure compare module to trigger ADC sample and timer 1 reset */
    CCP2CON &= ~(0x0F);
    CCP2CON |= 0x0B;    // enable special mode

    T3CONbits.T3CCP2 = 0;
    T3CONbits.T3CCP1 = 0; // set timer 1 as clock source for both CCP modules

    adc_set_sampling_rate(8000);

    // enable interrupts
    IPR1bits.ADIP = 0; // low priority
    PIE1bits.ADIE = 1; // enable interrupts


    T1CONbits.TMR1ON = 1; // enable timer 1
}

/* Set the compare value used to trigger an ADC read. As the value is
 * compared to Timer1's current state, the sampling rate is linked to
 * Timer1's frequency, which is Fosc/4
 */
void adc_set_sampling_rate(unsigned int rate)
{
    unsigned int sample_period = (CLOCK_FREQ / 4) / rate;
    CCPR2H = (sample_period >> 8) & 0xFF;
    CCPR2L = sample_period & 0xFF;
}

void adc_isr(void)
{
    if(PIR1bits.ADIF == 1) {
        // FIXME: make RD1 pulse with ADC read, frequency can be checked
        // with logic analyser / scope
        LATDbits.LATD1 = 1;
        TRISDbits.TRISD1 = 0;

        PIR1bits.ADIF = 0;
        
        LATDbits.LATD1 = 0;
    }

}