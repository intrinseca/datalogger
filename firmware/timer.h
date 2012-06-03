/*
 * Timer system to allow easy implementation of multiple timers using only a
 * single timer hardware module. The hardware timer is configured for 1ms
 * interrupts (i.e. like an OS tick timer). On each interrupt, all 'timer'
 * variables are decrementated. To measure a period of time (e.g. 12ms), load
 * the relevant varaiable with a value of '12'. The value of the variable can
 * be periodically checked (e.g. in the main loop), with a final value of '0'
 * indicating that the 'time is up'.
 */

#ifndef TIMER_H
#define TIMER_H

void timer_init(void);
void timer_isr(void);

extern volatile unsigned short watchdog_cntr;

#endif TIMER_H