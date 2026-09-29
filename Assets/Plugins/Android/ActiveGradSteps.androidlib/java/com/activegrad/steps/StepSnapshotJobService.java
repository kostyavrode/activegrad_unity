package com.activegrad.steps;

import android.app.job.JobParameters;
import android.app.job.JobService;
import android.hardware.Sensor;
import android.hardware.SensorEvent;
import android.hardware.SensorEventListener;
import android.hardware.SensorManager;
import android.os.Handler;
import android.os.Looper;

/**
 * Фоновый снимок счётчика шагов, пока игра закрыта.
 * Коротко слушает датчик, записывает значение и сразу отписывается.
 */
public class StepSnapshotJobService extends JobService {
    private static final long LISTEN_TIMEOUT_MS = 15_000L;

    private SensorManager sensorManager;
    private SensorEventListener listener;
    private final Handler handler = new Handler(Looper.getMainLooper());

    @Override
    public boolean onStartJob(final JobParameters params) {
        if (!StepCounterBridge.hasPermission(this)) {
            return false;
        }

        sensorManager = (SensorManager) getSystemService(SENSOR_SERVICE);
        Sensor sensor = sensorManager != null ? sensorManager.getDefaultSensor(Sensor.TYPE_STEP_COUNTER) : null;
        if (sensor == null) {
            return false;
        }

        listener = new SensorEventListener() {
            @Override
            public void onSensorChanged(SensorEvent event) {
                StepLedger.record(StepSnapshotJobService.this, (long) event.values[0]);
                finish(params);
            }

            @Override
            public void onAccuracyChanged(Sensor s, int accuracy) { }
        };

        sensorManager.registerListener(listener, sensor, SensorManager.SENSOR_DELAY_NORMAL);

        // Некоторые датчики присылают событие только после нового шага — не держим задачу вечно
        handler.postDelayed(new Runnable() {
            @Override
            public void run() {
                finish(params);
            }
        }, LISTEN_TIMEOUT_MS);

        return true;
    }

    @Override
    public boolean onStopJob(JobParameters params) {
        unregister();
        return false;
    }

    private void finish(JobParameters params) {
        if (listener == null) {
            return;
        }
        unregister();
        jobFinished(params, false);
    }

    private void unregister() {
        handler.removeCallbacksAndMessages(null);
        if (sensorManager != null && listener != null) {
            sensorManager.unregisterListener(listener);
        }
        listener = null;
    }
}
