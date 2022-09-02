'''
****************************************************************************************
File                db_setup.py - Ontario One Call Challenge
Description         Python script to import the data from the excel files to MySQL db 
                    Import excel was not working for me, hence this script was created.
Author              Shoaib Ali
Date                08/22/2022
Last Edited         08/28/2022
Additional Remarks  The virtual environment with required libs is quite large so I have
                    included a requirements.txt file to install the required additional 
                    libs via pip package manager.

                    manual usage (these are automatically done by easy-install.bat):
                    python -m venv .venv
                    ./.venv/Scripts/activate.bat
                    python -m pip install -r ./requirements.txt
                    python db_setup.py
**************************************************************************************** 
'''

import pandas as pd
import mysql.connector
from mysql.connector import MySQLConnection


'''
Main method contains an easy 'one click' database setup
'''
def main():
    ticket_info = './ticketInfo.xlsx'
    master_code = './MasterStationCode.xlsx'
    create_script = './db_setup.sql'
    db_name = "dbOntarioOneTest"

    # read from excel file, headers are on line 1, (ignore them)
    # convert the date objects to strings since they will be converted when inserting anyways
    df_ticket_info = pd.read_excel(ticket_info, header=0, converters={'processed_date': str, 'renegotiated_date': str, 'closed_date': str})

    # read from excel file
    df_master_code = pd.read_excel(master_code, header=0)

    try:
        # Init the connection, passing local db params
        connection: MySQLConnection = mysql.connector.connect(host='localhost', user='root', password='')
        if connection.is_connected():
            # Get some info about the connection
            db_Info = connection.get_server_info()
            print("Connected to MySQL Server version ", db_Info)
            cursor = connection.cursor()

            # Read the create/setup script from file and execute
            print(f"Running: Setup script ({create_script}) contains db/table create and stored procedure queries...", end='')
            with open(create_script, 'r') as f: sql_script = f.read().strip()

            # We split a maximum of 7 times because there is a semi colon that is part
            # of the stored procedure subquery, so we want to ignore that one.
            create_queries = sql_script.split(';', 7)
            for q in create_queries:
                cursor.execute(q)
            print("[OK]\n")

            # Once the script creates the ontarioonetest db, connect to it
            connection.database = db_name

            # Get db info
            cursor.execute("select database();")
            record = cursor.fetchone()
            print(f"Connected to database: {record[0]}")
            
            # Build the entire query by formatting and appending to a single string
            print(f"Running: Build insert query string for tables and insert records...", end='')
            sql_insert_records_query =  """USE dbOntarioOneTest;
                                        INSERT INTO tblTicketInfo
                                        VALUES """
            
            # Temp string to build out the values portion of the query; convert to list and then just string replace formatting
            tmpInsert = ""
            for item_list in df_ticket_info.values.tolist():
                tmpInsert += f"{str(item_list).replace('[', '(').replace(']', ')').replace('nan', 'NULL').replace(' 00:00:00', '')},"

            # Remove that trailing comma and replace it with a semi colon to complete the insert
            sql_insert_records_query += tmpInsert.rstrip(tmpInsert[-1]) + ';'

            # Line break and start tblmasterstationcode insert
            sql_insert_records_query += """\nINSERT INTO tblMasterStationCode
                                        VALUES """
            
            # Temp string to build out the values portion of the query; convert to list and then just string replace formatting
            tmpInsert = ""
            for item_list in df_master_code.values.tolist():
                tmpInsert += f"{str(item_list).replace('[', '(').replace(']', ')')},"
            
            # Remove that trailing comma and replace it with a semi colon to complete the insert
            sql_insert_records_query += tmpInsert.rstrip(tmpInsert[-1]) + ';'

            # One entire query has been built up, execute 
            cursor.execute(sql_insert_records_query)
            record = cursor.fetchone()
            if record == None:
                print(f"[OK]")
            else:
                print(record)

            # Close the connection, script ends
            cursor.close()
            connection.close()
            print("\nMySQL connection is closed, script will now exit.")
            exit()

    except mysql.connector.Error as e:
        print("Error while connecting to MySQL", e)


if __name__ == '__main__':
    main()
