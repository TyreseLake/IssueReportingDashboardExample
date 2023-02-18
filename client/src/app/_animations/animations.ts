import { trigger, state, style, transition, animate, keyframes, query, stagger } from '@angular/animations';

export const fadeInOut = trigger('fadeInOut', [
  transition(':enter', [
    style({ opacity: 0, transform: 'translateY(-5%)' }),
    animate('100ms ease-in', style({ opacity: 1, transform: 'translateY(0)' })),
  ]),
  transition(':leave', [
    animate('100ms ease-out', style({ opacity: 0, transform: 'translateY(-5%)' }))
  ])
]);

export const fadeInStagger = trigger('fadeInStagger', [
  transition(':enter', [
    query(':enter', [
      style({ opacity: 0 , transform: 'translateY(10%)'}),
      stagger('10ms', [animate('200ms ease-in', style({ opacity: 1, transform: 'translateY(0)' }))])
    ])
  ])
]);

export const fadeInOutGrow = trigger('fadeInOutGrow', [
  transition(':enter', [
    style({ opacity: 0, transform: 'scale(80%)' }),
    animate('300ms cubic-bezier(0.35, 0, 0.25, 1)', style({ opacity: 1, transform: 'scale(100%)' })),
  ]),
  transition(':leave', [
    animate('300ms cubic-bezier(0.35, 0, 0.25, 1)', style({ opacity: 0, transform: 'scale(80%)' }))
  ])
]);

export const slideInOut  = trigger('slideInOut', [
  transition(':enter', [
    style({transform: 'translateX(+100%)'}),
    animate('200ms ease-in', style({transform: 'translateX(0%)'}))
  ]),
]);
