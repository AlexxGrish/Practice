cd /d "%~dp0"

git add .

set "safe_time=%time::=-%"


set "safe_time=%safe_time: =0%"


git commit -m "%date% %safe_time%"


git push

echo.
echo Done
pause