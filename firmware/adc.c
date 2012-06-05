/* ADC sampling code.
 *
 * This code is responsible for setup and sampling of the ADC, including the
 * use of a timer module for accurate timing.
 */

#include <p18f4550.h>
#include <string.h>

#include "adc.h"
#include "pool.h"

#include "HardwareProfile.h"
#include "isr.h"

unsigned char * curr_buff;
volatile unsigned char next_free_pos;

volatile unsigned char * filled_buff;
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
    unsigned char sample;
    
    if(PIR1bits.ADIF == 1) {
        // FIXME: make RD1 pulse with ADC read, frequency can be checked
        // with logic analyser / scope
        LATDbits.LATD1 = 1;
        TRISDbits.TRISD1 = 0;

        PIR1bits.ADIF = 0;

        /* If there's space in the buffer, copy the new sample into it. If the
         * current buffer is full, allocate a new one, and mark the old one as
         * filled. If a new buffer cannot be allocated, drop the sample (and
         * increment the sample overrun count). FIXME implement all of this
         */

        /* read the sample */
        sample = ((ADRESH << 6) & 0xC0) | ((ADRESL >> 2) & 0x3F);

        /* check if current buffer has space */
        if(next_free_pos < POOL_BUFF_SIZE) {
            /* no buffer management needed */
        } else {
            /* buffer is full. If the previous buffer has already been 'claimed'
             * some other system (e.g. USB), allocate a new buffer and mark the
             * current one as full. If the previous buffer has not been claimed,
             * then just refill the current buffer. If the old one has been
             * claimed, but we cannot allocate a new buffer, reuse current
             * buffer, and mark overrun.
             */
            if(filled_buff == NULL) { // a.k.a has been claimed
                unsigned char * new_buff;
                new_buff = pool_malloc_buff();

                if(new_buff != NULL) { // successfully got a new buffer
                    filled_buff = (volatile void *) curr_buff;
                    curr_buff = new_buff;
                    next_free_pos = 0;
                } else { // no free buffers available, reuse existing buffer
                    next_free_pos = 0;  // this represents an overrun
                    // FIXME: count overrun
                }
            } else { // previous buffer hasn't been claimed, just rewrite
                next_free_pos = 0;
            }
        }

        // copy sample into buffer
        curr_buff[next_free_pos] = sample;
        next_free_pos++;
        
        LATDbits.LATD1 = 0;
    }

}

void adc_tick(void)
{
    
}

void * adc_get_filled_buff(void)
{
    void * ret;

    isr_disable_interrupts();
        ret = (void *) filled_buff;
        filled_buff = NULL;
    isr_enable_interrupts();
    
    return ret;
}

void adc_free_buff(void * buff)
{
    pool_free_buff(buff);
}

/*
 * Gets a new buffer from the pool and sets internal state. Returns 0 on
 * success, otherwise -1
 */
unsigned char get_new_buff(void)
{
    void * new_buff;

    new_buff = pool_malloc_buff();
    
    if(new_buff == NULL) {
        return -1;  // no buffers available
    }

    curr_buff = new_buff;
    next_free_pos = 0;
}