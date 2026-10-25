#!/system/bin/sh
until [ "$(getprop sys.boot_completed)" = "1" ]; do sleep 2; done
for p in RECORD_AUDIO READ_PHONE_STATE READ_CALL_LOG READ_CONTACTS ANSWER_PHONE_CALLS CALL_PHONE POST_NOTIFICATIONS; do
	pm grant dev.callbridge android.permission.$p
done
