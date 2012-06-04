#ifndef BUFFER_H
#define BUFFER_H

void * pool_malloc_buff(void);
void pool_free_buff(void * handle);
void pool_init(void);

#endif