/* audio buffer memory pool
 *
 * This code is responsible for managing a pool of USB buffers, for use within
 * the ADC and USB system. Buffers can be retrieved and added to the pool, with
 * a handle used to track access. This is extremely close to 'malloc', however
 * because of the greater restrictions imposed upon chunk sizes, will never
 * fragment. Furthermore, it makes use of the unusual allocation syntax required
 * for USB allocated memory.
 */

#include <p18f4550.h>
#include <string.h>

#include "pool.h"

#define NUM_BUFFERS 8

enum ALLOCED_STATUS {
    BUFF_FREE,
    BUFF_NOT_FREE
};

#pragma udata USB_VARS
unsigned char pool[NUM_BUFFERS][POOL_BUFF_SIZE];
#pragma udata

#pragma code

unsigned char alloced[NUM_BUFFERS];

/*
 * Initialise the memory pool. Set all buffers to be 'free' and zero out all
 * arrays.
 */
void pool_init(void)
{
    unsigned char i;

    for(i = 0; i < NUM_BUFFERS; ++i) {
        memset(pool[i], 0, POOL_BUFF_SIZE);
        alloced[i] = BUFF_FREE;
    }
}

/*
 * Retrieves a new buffer from the pool. Returns the address of the new buffer,
 * or NULL if a buffer failed to be allocated.
 */
void * pool_malloc_buff(void)
{
    unsigned char i;

    /* try and find an unallocated buffer */
    for(i = 0; i < NUM_BUFFERS; ++i) {
        if(alloced[i] == BUFF_FREE) {
            alloced[i] = BUFF_NOT_FREE;
            return pool[i];
        }
    }

    return NULL; // all buffers allocated
}

void pool_free_buff(void * handle)
{
    unsigned char i;

    for(i = 0; i < NUM_BUFFERS; ++i) {
        if(pool[i] == handle) {
            alloced[i] = BUFF_FREE;
            return;
        }
    }

    return; // FIXME: check: we should never get here.
}
