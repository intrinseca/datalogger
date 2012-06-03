/* Watchdog task
 *
 * Task to 'blink' the onboard LED at 1Hz, as an indicator of system stability.
 * Timing is achieved by using the 'timer' subsystem (see timer.c). The LED
 * failing to blink is an indicator that the main loop has stopped, or is not
 * running sufficiently quickly.
 */

#include <p18f4550.h>
#include "timer.h"

unsigned char state;

void watchdog_init()
{
    state = 0;
    
    // Enable RD0 as an output
    LATDbits.LATD0 = 0;
    TRISDbits.TRISD0 = 0;
}

/* Toggles LED at 1Hz, with an 'on' period of 50ms */
void watchdog_tick()
{
    isr_disable_interrupts();

    if(watchdog_cntr == 0) {
        if(state == 0) {
            watchdog_cntr = 50;
            LATDbits.LATD0 = 1;
            state = 1;
        } else {
            watchdog_cntr = 950;
            LATDbits.LATD0 = 0;
            state = 0;
        }
    }

    isr_enable_interrupts();
}
