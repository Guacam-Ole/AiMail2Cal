# AiMail2Cal

This is a small simple tool that uses the OpenAi-Api to detect dates in emails read via Imap. If a date is found a new Calendar-Entry is created in a Calendar using CalDav (e.g. NextCloud-Calendars)
The bot will add the following information to an appointment if it can be found:
* Start-date of the appointment
* End-date of the appointment
* Location
* Participants

## Config
Simply copy secrets.example.json to secrets.json and enter your values. You will need an openAi-Api-Key, Credentials for your Imap server and Credentials for your calDav-Calendar
