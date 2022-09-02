-- File                db_setup.sql - Ontario One Call Challenge
-- Description         Main sql queries for initial database setup 
-- Author              Shoaib Ali
-- Date                08/22/2022
-- Last Edited         08/28/2022

CREATE DATABASE IF NOT EXISTS dbOntarioOneTest;
USE dbOntarioOneTest;
DROP TABLE IF EXISTS tblTicketInfo;
DROP TABLE IF EXISTS tblMasterStationCode;

CREATE TABLE tblMasterStationCode (
    master_station_code VARCHAR(20),
    member_code VARCHAR(20)
);

CREATE TABLE tblTicketInfo (
    processed_date DATE,
    ticket_number BIGINT NOT NULL,
    member_code VARCHAR(20),
    master_code VARCHAR(20),
    renegotiated_date DATE,
    closed_date DATE,
    day_to_close INT,
    time_to_respond VARCHAR(10),
    compliance INT
);

DROP PROCEDURE IF EXISTS `sp_OntarioOneCallChallenge`;

CREATE DEFINER=`root`@`localhost` PROCEDURE `sp_OntarioOneCallChallenge`(IN member VARCHAR(50))
BEGIN
	SELECT sqDayToClose.day_to_close,
		IF(sqDayToClose.day_to_close < 5, '0-5', 
			IF(sqDayToClose.day_to_close < 11, '5-10',
				IF(sqDayToClose.day_to_close < 16, '11-15', 
					IF(sqDayToClose.day_to_close > 16, '15+',
						'Unknown'
					)
				)
			)
		) as time_to_respond,
		IF(sqDayToClose.day_to_close < 5, 'Compliant', 'Non-Compliant') as compliance

		FROM (
			SELECT
				IF(renegotiated_date IS NULL AND closed_date IS NULL, DATEDIFF(CURRENT_DATE, processed_date),
					IF(renegotiated_date IS NULL, DATEDIFF(closed_date, processed_date), 
						DATEDIFF(closed_date, renegotiated_date)
					)
				) as day_to_close
			FROM 
				`tblticketinfo`

			WHERE 
				member_code=member

		) as sqDayToClose;
END