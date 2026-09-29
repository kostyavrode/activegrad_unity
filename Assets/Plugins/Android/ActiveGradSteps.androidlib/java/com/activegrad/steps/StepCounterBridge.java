package com.activegrad.steps;

import android.Manifest;
import android.app.job.JobInfo;
import android.app.job.JobScheduler;
import android.content.ComponentName;
import android.content.Context;
import android.content.pm.PackageManager;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Build;

/**
 * Точка входа для Unity (вызывается через AndroidJavaClass).
 */
public final class StepCounterBridge {
    private static final int JOB_ID = 0x5739;
    private static final long JOB_INTERVAL_MS = 15L * 60L * 1000L;

    private static SensorEventListener foregroundListener;

    private StepCounterBridge() { }

    public static boolean isSensorAvailable(Context context) {
        SensorManager sm = (SensorManager) context.getSystemService(Context.SENSOR_SERVICE);
        return sm != null && sm.getDefaultSensor(Sensor.TYPE_STEP_COUNTER) != null;
    }

    public static boolean hasPermission(Context context) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.Q) {
            return true;
        }
        return context.checkSelfPermission(Manifest.permission.ACTIVITY_RECOGNITION) == PackageManager.PERMISSION_GRANTED;
    }

    public static boolean needsRuntimePermission() {
        return Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q;
    }

    /** Подписка на датчик, пока игра запущена, и планирование фоновых снимков. */
    public static synchronized boolean start(Context context) {
        final Context app = context.getApplicationContext();
        if (!isSensorAvailable(app) || !hasPermission(app)) {
            return false;
        }

        scheduleBackgroundSnapshots(app);

        if (foregroundListener != null) {
            return true;
        }

        SensorManager sm = (SensorManager) app.getSystemService(Context.SENSOR_SERVICE);
        Sensor sensor = sm.getDefaultSensor(Sensor.TYPE_STEP_COUNTER);
        foregroundListener = new SensorEventListener() {
            @Override
            public void onSensorChanged(SensorEvent event) {
                StepLedger.record(app, (long) event.values[0]);
            }

            @Override
            public void onAccuracyChanged(Sensor s, int accuracy) { }
        };
        sm.registerListener(foregroundListener, sensor, SensorManager.SENSOR_DELAY_NORMAL);
        return true;
    }

    public static synchronized void stop(Context context) {
        if (foregroundListener == null) {
            return;
        }
        SensorManager sm = (SensorManager) context.getApplicationContext().getSystemService(Context.SENSOR_SERVICE);
        if (sm != null) {
            sm.unregisterListener(foregroundListener);
        }
        foregroundListener = null;
    }

    public static long getStepsToday(Context context) {
        return StepLedger.getStepsToday(context);
    }

    public static long getLastUpdateMillis(Context context) {
        return StepLedger.getLastUpdateMillis(context);
    }

    private static void scheduleBackgroundSnapshots(Context context) {
        JobScheduler scheduler = (JobScheduler) context.getSystemService(Context.JOB_SCHEDULER_SERVICE);
        if (scheduler == null || scheduler.getPendingJob(JOB_ID) != null) {
            return;
        }

        JobInfo job = new JobInfo.Builder(JOB_ID, new ComponentName(context, StepSnapshotJobService.class))
                .setPeriodic(JOB_INTERVAL_MS)
                .setPersisted(true)
                .build();
        scheduler.schedule(job);
    }
}
