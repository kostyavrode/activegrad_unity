package com.activegrad.steps;

import android.content.Context;
import android.content.SharedPreferences;
import android.os.SystemClock;

import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.Date;
import java.util.Locale;

/**
 * Журнал шагов за день поверх аппаратного счётчика TYPE_STEP_COUNTER.
 *
 * Счётчик отдаёт шаги с момента загрузки телефона, поэтому храним последний снимок
 * и прибавляем разницу. Снимки делаются, пока игра открыта, и фоновой задачей раз в ~15 минут.
 */
final class StepLedger {
    private static final String PREFS = "activegrad_steps";
    private static final String K_DAY = "day";
    private static final String K_STEPS = "steps";
    private static final String K_LAST_COUNTER = "last_counter";
    private static final String K_LAST_TIME = "last_time";
    private static final String K_BOOT_TIME = "boot_time";
    private static final String K_PREV_DAY = "prev_day";
    private static final String K_PREV_STEPS = "prev_steps";

    // Время загрузки вычисляется с погрешностью, поэтому сравниваем с запасом
    private static final long BOOT_TIME_TOLERANCE_MS = 60_000L;

    private StepLedger() { }

    static synchronized void record(Context context, long counter) {
        if (counter < 0) {
            return;
        }

        SharedPreferences prefs = prefs(context);
        long now = System.currentTimeMillis();
        long bootTime = now - SystemClock.elapsedRealtime();
        String today = dayKey(now);
        long startOfToday = startOfDay(now);

        long lastCounter = prefs.getLong(K_LAST_COUNTER, -1L);
        long lastTime = prefs.getLong(K_LAST_TIME, 0L);
        long lastBoot = prefs.getLong(K_BOOT_TIME, 0L);
        String day = prefs.getString(K_DAY, null);
        long steps = prefs.getLong(K_STEPS, 0L);

        SharedPreferences.Editor editor = prefs.edit();

        if (lastCounter < 0 || day == null) {
            // Первый запуск: если телефон загрузился сегодня, весь счётчик — это шаги за сегодня
            steps = bootTime >= startOfToday ? counter : 0L;
            day = today;
        } else {
            boolean rebooted = Math.abs(bootTime - lastBoot) > BOOT_TIME_TOLERANCE_MS || counter < lastCounter;
            long delta = rebooted ? counter : counter - lastCounter;
            if (delta < 0) {
                delta = 0;
            }

            if (!today.equals(day)) {
                // Наступил новый день: делим шаги между снимками пропорционально времени после полуночи
                long afterMidnight = delta;
                if (lastTime > 0 && lastTime < startOfToday && now > lastTime) {
                    double fraction = (now - startOfToday) / (double) (now - lastTime);
                    afterMidnight = Math.round(delta * fraction);
                }

                editor.putString(K_PREV_DAY, day);
                editor.putLong(K_PREV_STEPS, steps + (delta - afterMidnight));

                steps = afterMidnight;
                day = today;
            } else {
                steps += delta;
            }
        }

        editor.putString(K_DAY, day);
        editor.putLong(K_STEPS, steps);
        editor.putLong(K_LAST_COUNTER, counter);
        editor.putLong(K_LAST_TIME, now);
        editor.putLong(K_BOOT_TIME, bootTime);
        editor.apply();
    }

    static synchronized long getStepsToday(Context context) {
        SharedPreferences prefs = prefs(context);
        String today = dayKey(System.currentTimeMillis());
        return today.equals(prefs.getString(K_DAY, null)) ? prefs.getLong(K_STEPS, 0L) : 0L;
    }

    static synchronized long getLastUpdateMillis(Context context) {
        return prefs(context).getLong(K_LAST_TIME, 0L);
    }

    private static SharedPreferences prefs(Context context) {
        return context.getApplicationContext().getSharedPreferences(PREFS, Context.MODE_PRIVATE);
    }

    private static String dayKey(long millis) {
        return new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(new Date(millis));
    }

    private static long startOfDay(long millis) {
        Calendar calendar = Calendar.getInstance();
        calendar.setTimeInMillis(millis);
        calendar.set(Calendar.HOUR_OF_DAY, 0);
        calendar.set(Calendar.MINUTE, 0);
        calendar.set(Calendar.SECOND, 0);
        calendar.set(Calendar.MILLISECOND, 0);
        return calendar.getTimeInMillis();
    }
}
