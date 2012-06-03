/*
 * Timer system to allow easy implementation of multiple timers using only a
 * single timer hardware module. The hardware timer is configured for 1ms
 * interrupts (i.e. like an OS tick timer). On each interrupt, all 'timer'
 * variables are decrementated. To measure a period of time (e.g. 12ms), load
 * the relevant varaiable with a value of '12'. The value of the variable can
 * be periodically checked (e.g. in the main loop), with a final value of '0'
 * indicating that the 'time is up'.
 */

#include <p18f4550.h>

/* timer constants */
#define CLK_HZ  48000000 // FIXME: this could be determined from somewhere
#define TICK_HZ 1000

#define TMR_PRESCALE    1
#define TMR_RELOAD_VAL  (65536 - ((CLK_HZ / (4 * TMR_PRESCALE)) / TICK_HZ))

/* forward declerations */
static void reload_timer(void);
static void update_counters(void);

/* counter variables */
volatile unsigned short watchdog_cntr;

/*
 * Initialise the hardware timer system and reset all internal state
 */
void timer_init(void)
{
    watchdog_cntr = 0;

    INTCONbits.TMR0IE = 0; // ensure interrupt is disabled
    INTCONbits.TMR0IF = 0; // clear any pending interrupt

    T0CONbits.T08BIT = 0; // enable 16-bit mode operation

    T0CONbits.T0CS = 0; // use internal oscillator as clock source

    /* no prescaling */
    T0CONbits.T0PS2 = 0;
    T0CONbits.T0PS1 = 0;
    T0CONbits.T0PS0 = 0;
    T0CONbits.PSA = 1;  // bypass prescalar completely

    T0CONbits.TMR0ON = 0; // ensure counter is stopped
    reload_timer();     // prepare the timer count

    INTCON2bits.TMR0IP = 0; // set timer to low priority interrupt
    INTCONbits.TMR0IE = 1; // re-enable interrupts
    
    T0CONbits.TMR0ON = 1; // start the counters
}

/*
 * Timer interrupt service routine to handle decrementing variables etc... As
 * this function cannot be directly registered to handle the interrupt (PIC
 * architecure limitation), this function will actually be called from the real
 * ISR. This incurs a slight overhead, but is worthwhile to help readability &
 * modularity of the code.
 */
void timer_isr(void)
{
    /* check to see if interrupt has occured */
    if(INTCONbits.TMR0IF == 1) {
        reload_timer();

        /* clear interrupt */
        INTCONbits.TMR0IF = 0;

        update_counters();
    }
}

/*
 * Reloads the timer value. As interrupts only occur on timer overflow (no
 * interrupt on compare), this function is needed to load a correct 'starting
 * value' to count from, such that overflow occurs after the required time
 * period.
 */
static void reload_timer(void)
{
    /* register writes must occur in this order explicitly, as TMR0H is actually
     * a shadow register, which is only updated upon write of TMR0L.
     */
    TMR0H = (TMR_RELOAD_VAL >> 8) & 0xFF;
    TMR0L = TMR_RELOAD_VAL & 0xFF;
}

static void update_counters(void)
{
    if(watchdog_cntr > 0) {
        watchdog_cntr--;
    }
}