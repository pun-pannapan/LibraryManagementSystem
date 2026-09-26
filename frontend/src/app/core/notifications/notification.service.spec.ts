import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('fades and dismisses a notification after three seconds', () => {
    const service = new NotificationService();
    service.show('success', 'Saved.');

    vi.advanceTimersByTime(2700);
    expect(service.current()?.fading).toBe(true);

    vi.advanceTimersByTime(300);
    expect(service.current()).toBeNull();
  });

  it('restarts the timers when a new notification is shown', () => {
    const service = new NotificationService();
    service.show('success', 'First');
    vi.advanceTimersByTime(2500);

    service.show('info', 'Second');
    vi.advanceTimersByTime(500);

    expect(service.current()).toEqual({ type: 'info', message: 'Second', fading: false });
  });
});
