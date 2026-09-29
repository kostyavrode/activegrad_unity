#import <Foundation/Foundation.h>
#import <CoreMotion/CoreMotion.h>

// Шаги за сегодня через CoreMotion. iOS сам хранит историю шагов за 7 дней,
// поэтому данные есть даже за время, когда игра не запускалась.

static CMPedometer *ag_pedometer = nil;
static NSDate *ag_dayStart = nil;
static NSInteger ag_stepsToday = -1;
static BOOL ag_denied = NO;

static NSDate *AGStartOfToday(void)
{
    return [[NSCalendar currentCalendar] startOfDayForDate:[NSDate date]];
}

static void AGHandleError(NSError *error)
{
    if (error != nil && [error.domain isEqualToString:CMErrorDomain] &&
        (error.code == CMErrorMotionActivityNotAuthorized || error.code == CMErrorMotionActivityNotAvailable))
    {
        ag_denied = YES;
    }
}

static void AGApplySteps(CMPedometerData *data, NSDate *forDayStart)
{
    if (data == nil)
        return;

    NSInteger steps = data.numberOfSteps.integerValue;
    dispatch_async(dispatch_get_main_queue(), ^{
        // Игнорируем ответы за прошлый день, пришедшие после полуночи
        if (ag_dayStart != nil && [ag_dayStart isEqualToDate:forDayStart])
        {
            ag_stepsToday = MAX(ag_stepsToday, steps);
            ag_denied = NO;
        }
    });
}

static void AGRestartForToday(void)
{
    if (ag_pedometer == nil)
        ag_pedometer = [[CMPedometer alloc] init];

    [ag_pedometer stopPedometerUpdates];

    NSDate *dayStart = AGStartOfToday();
    ag_dayStart = dayStart;
    ag_stepsToday = -1;

    [ag_pedometer queryPedometerDataFromDate:dayStart toDate:[NSDate date] withHandler:^(CMPedometerData *data, NSError *error) {
        AGHandleError(error);
        AGApplySteps(data, dayStart);
    }];

    [ag_pedometer startPedometerUpdatesFromDate:dayStart withHandler:^(CMPedometerData *data, NSError *error) {
        AGHandleError(error);
        AGApplySteps(data, dayStart);
    }];
}

extern "C" {

bool _AGPedometer_IsAvailable()
{
    return [CMPedometer isStepCountingAvailable];
}

// 0 — не спрашивали, 1 — ограничено, 2 — запрещено, 3 — разрешено
int _AGPedometer_AuthorizationStatus()
{
    if (@available(iOS 11.0, *))
        return (int)[CMPedometer authorizationStatus];
    return ag_denied ? 2 : 3;
}

void _AGPedometer_Start()
{
    if (![CMPedometer isStepCountingAvailable])
        return;

    if (ag_dayStart == nil || ![ag_dayStart isEqualToDate:AGStartOfToday()])
        AGRestartForToday();
}

// Повторный запрос истории — после возврата игры из фона
void _AGPedometer_Refresh()
{
    if (![CMPedometer isStepCountingAvailable])
        return;

    if (ag_dayStart == nil || ![ag_dayStart isEqualToDate:AGStartOfToday()])
    {
        AGRestartForToday();
        return;
    }

    NSDate *dayStart = ag_dayStart;
    [ag_pedometer queryPedometerDataFromDate:dayStart toDate:[NSDate date] withHandler:^(CMPedometerData *data, NSError *error) {
        AGHandleError(error);
        AGApplySteps(data, dayStart);
    }];
}

// -1, если данных ещё нет
int _AGPedometer_GetStepsToday()
{
    if (ag_dayStart != nil && ![ag_dayStart isEqualToDate:AGStartOfToday()])
        AGRestartForToday();

    return (int)ag_stepsToday;
}

}
