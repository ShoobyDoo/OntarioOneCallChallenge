Ontario One Call Challenge Submission


First steps/Installation
------------------------

Main Requirement - Install Python 3.10.5 [https://www.python.org/ftp/python/3.10.5/python-3.10.5-amd64.exe]
	- When installing, ensure 'Add Python to Path' is checked.

	Note: Script 'db_setup.py' has not been tested on other Python versions.

Note: Please ensure that MySQL server is running with the default credentials;
	username: root
	no password

------------------------

Once Python is installed, simply double click ./OntarioOneCallChallenge/Setup/easy-install.bat which will do the following:
	1. Create a virtual environment for Python so as not to tamper with your main installation.
	2. Activate the virtual environment, read from requirements.txt and download libraries required by the script.
	3. Once all the dependencies have been resolved, the db_setup.py script will run.
		a. This script will automatically read the excel files, create the database, tables, and store procedure, then exit.
	4. Once the database setup script has completed, the main OntarioOneCallChallenge executable will start.

------------------------

That is pretty much it; the program will be reading from the created database and displaying the appropriate information.

I tried to use as much of the space as possible in an intellegent manner, so I moved the buttons around, 
as well as how the table is displayed in order to achieve a nicer UX/UI.

------------------------

All in all, thank you very much for this opportunity, it was an interesting and engaging project. 
I hope to hear back from you soon!

Regards, 
Shoaib Ali