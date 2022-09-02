:: File:	Simple one click bat file to get all the required libs for python and run the script.
:: Author:	Shoaib Ali
:: Date:	08/28/2022

@ECHO OFF

ECHO Setting up environment, please wait...
python -m venv .venv
call .venv\Scripts\activate
ECHO Virtual environment created and active, installing libraries...
ECHO.
python -m pip install -r requirements.txt
ECHO.
ECHO Libraries installed. Starting database setup script (db_setup.py)...
ECHO.
python db_setup.py
ECHO.
ECHO Database setup is complete. Starting OntarioOneCallChallenge.exe...
ECHO.
ECHO.
start ..\OntarioOneCallChallenge.exe
PAUSE
