#!/system/bin/sh
until [ "$(getprop sys.boot_completed)" = "1" ]; do sleep 2; done
for p in CAMERA RECORD_AUDIO READ_PHONE_STATE READ_CALL_LOG READ_CONTACTS ANSWER_PHONE_CALLS CALL_PHONE POST_NOTIFICATIONS; do
	pm grant dev.callbridge android.permission.$p
done
appops set dev.callbridge SYSTEM_ALERT_WINDOW allow
cmd appops set dev.callbridge 10008 allow
dumpsys deviceidle whitelist +dev.callbridge
waited=0
until [ "$(getprop sys.user.0.ce_available)" = "true" ] || [ $waited -ge 3600 ]; do sleep 5; waited=$((waited + 5)); done
cmd appops set dev.callbridge 10008 allow
am broadcast -a android.intent.action.BOOT_COMPLETED -n dev.callbridge/.StartReceiver
