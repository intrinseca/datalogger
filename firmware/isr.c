/* Interrupt service routines.
 *
 * As the PIC ISR architecture is fairly simple, this file is used to 'hook'
 * all peripheral ISRs into the global ISRs.
 */

#include <p18f4550.h>

#include "isr.h"
#include "timer.h"

/* forward declerations */
void high_priority_isr(void);
void low_priority_isr(void);

#pragma code _HIGH_INTERRUPT_VECTOR = 0x000808
void _high_ISR (void)
{
    _asm goto high_priority_isr _endasm
}

#pragma code _LOW_INTERRUPT_VECTOR = 0x000818
void _low_ISR (void)
{
    _asm goto low_priority_isr _endasm
}
#pragma code

/* Real ISRs. These are needed as there is insufficient code space to fit proper
 * ISRs at the required memory locations. Therefore they are used merely as a
 * jump pad to reach these functions.
 */
#pragma interrupt high_priority_isr
void high_priority_isr(void)
{
    USBDeviceTasks(); // must be high priority (discovered by reading library code
}

#pragma interruptlow low_priority_isr
void low_priority_isr(void)
{
    adc_isr();
    timer_isr();
}
/*
 * Initialise the interrupt system. This includes setup of the interrupt
 * priority feature etc..
 */
void isr_init(void)
{
    isr_disable_interrupts();

    /* disable all interrupt sources */
    INTCONbits.TMR0IE = 0;
    INTCONbits.INT0E = 0;
    INTCONbits.RBIE = 0;
    INTCON3bits.INT2IE = 0;
    INTCON3bits.INT1IE = 0;
    // including peripheral interrupts
    PIE1 = 0;
    PIE2 = 0;

    RCONbits.IPEN = 1;  // enable 2-level interrupt priority

    /* mark all interrupts as low priority */
    INTCON2bits.TMR0IP = 0;
    INTCON2bits.RBIP = 0;
    INTCON3bits.INT2IP = 0;
    INTCON3bits.INT1IP = 0;
    IPR1 = 0;
    IPR2 = 0;

    isr_enable_interrupts();
}

/*
 * Global disable interrupts.
 *
 * Typically used to check volatile variables atomically without possible
 * interrupts.
 */
void isr_disable_interrupts(void)
{
    INTCONbits.GIEH = 0;    // disable all interrupts
    INTCONbits.GIEL = 0;
}

/*
 * Global enable interrupts.
 *
 * Typically used to check volatile variables atomically without possible
 * interrupts.
 */
void isr_enable_interrupts(void)
{
    INTCONbits.GIEH = 1;    // globally re-enable interrupts
    INTCONbits.GIEL = 1;
}