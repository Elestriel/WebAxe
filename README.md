# WebexKiller
Webex likes to leave a **lot** of stuff running on your computer when you're done using it. These things it leaves running are services that can automatically re-launch Webex when you don't want it open. They are invasive, and Cisco have made no comment on the matter.

This tool is designed to simply kill those processes.

# How does it work?
This little tool sits in your System Tray and watches for the Webex processes that like to stay running, even when you're done with your meetings. 
Specifically, it looks for `atmgr.exe`, `ptoneclk.exe`, and `webexmta.exe`.

# Options
**Watch Processes** - This will scan your running processes once every 60 seconds. With this disabled, scanning is disabled and only manual cleanups can occur.

**Automatically End Processes** - When enabled, automatically destroys Webex processes when it detects that `ptoneclk.exe` isn't running. This is the Cisco Webex Meetings app that appears after meetings. **This has not been extensively tested yet and could kill your meetings**

When disabled, shows a little Toast notification tha allows you to kill the processes or snooze for one hour. While snoozed, scanning is paused and nothing can be killed automatically. Manual cleanups can still be done, however.

**Minimizing** When you minimize this application, it'll disappear from the Taskbar and sit in the System Tray, over by your clock.
