-- MySQL Workbench Forward Engineering

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='TRADITIONAL,ALLOW_INVALID_DATES';

-- -----------------------------------------------------
-- Schema IrrigationController
-- -----------------------------------------------------

-- -----------------------------------------------------
-- Schema IrrigationController
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `IrrigationController` DEFAULT CHARACTER SET utf8 ;
USE `IrrigationController` ;

-- -----------------------------------------------------
-- Table `IrrigationController`.`Command`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `IrrigationController`.`Command` (
  `CommandId` INT NOT NULL,
  `Title` VARCHAR(45) NOT NULL,
  `Description` VARCHAR(100) NULL,
  PRIMARY KEY (`CommandId`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `IrrigationController`.`CommandHistory`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `IrrigationController`.`CommandHistory` (
  `Id` INT NOT NULL,
  `CommandId` VARCHAR(45) NOT NULL,
  `Params` VARCHAR(100) NULL,
  `Issued` DATETIME NOT NULL,
  `Actioned` DATETIME NULL,
  PRIMARY KEY (`Id`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `IrrigationController`.`ControllerStatus`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `IrrigationController`.`ControllerStatus` (
  `Id` INT NOT NULL,
  `State` VARCHAR(45) NOT NULL,
  `Mode` VARCHAR(45) NULL,
  `TimeStamp` DATETIME NULL,
  `LowPressureFault` TINYINT(1) NULL,
  `HighPressureFault` TINYINT(1) NULL,
  `LowWellFault` TINYINT(1) NULL,
  `OverloadFault` TINYINT(1) NULL,
  `ResetRelay` TINYINT(1) NULL,
  `PumpRelay` TINYINT(1) NULL,
  `Station1Relay` TINYINT(1) NULL,
  `Station2Relay` TINYINT(1) NULL,
  `Station3Relay` TINYINT(1) NULL,
  `Station4Relay` TINYINT(1) NULL,
  `Station5Relay` TINYINT(1) NULL,
  `Station6Relay` TINYINT(1) NULL,
  `Station7Relay` TINYINT(1) NULL,
  `Station8Relay` TINYINT(1) NULL,
  `Station9Relay` TINYINT(1) NULL,
  `Station10Relay` TINYINT(1) NULL,
  `Station11Relay` TINYINT(1) NULL,
  `Station12Relay` TINYINT(1) NULL,
  PRIMARY KEY (`Id`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `IrrigationController`.`EventHistory`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `IrrigationController`.`EventHistory` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `TimeStamp` DATETIME NOT NULL,
  `EventType` INT NULL,
  `Description` VARCHAR(100) NULL,
  PRIMARY KEY (`Id`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `IrrigationController`.`EventType`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `IrrigationController`.`EventType` (
  `Id` INT NOT NULL,
  `Name` VARCHAR(45) NOT NULL,
  `Description` VARCHAR(100) NULL,
  PRIMARY KEY (`Id`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `IrrigationController`.`Schedule`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `IrrigationController`.`Schedule` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `StationId` INT NOT NULL,
  `Start` DATETIME NOT NULL,
  `Duration` INT NOT NULL,
  `Repeat` TINYINT(1) NULL,
  `Interval` INT NULL,
  `Enabled` VARCHAR(45) NULL,
  PRIMARY KEY (`Id`))
ENGINE = InnoDB;

USE `IrrigationController` ;

-- -----------------------------------------------------
-- Placeholder table for view `IrrigationController`.`vwPendingCommands`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `IrrigationController`.`vwPendingCommands` (`Id` INT, `CommandId` INT, `Params` INT, `Title` INT, `Issued` INT, `Description` INT);

-- -----------------------------------------------------
-- View `IrrigationController`.`vwPendingCommands`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `IrrigationController`.`vwPendingCommands`;
USE `IrrigationController`;
CREATE  OR REPLACE VIEW `vwPendingCommands` AS
SELECT 	h.Id,
		h.CommandId,
		h.Params,
        c.Title,
        h.Issued,
        c.Description
FROM CommandHistory h
INNER JOIN Command c on c.CommandId = h.CommandId
WHERE Actioned is NULL;

SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
-- begin attached script 'script'
INSERT INTO EventType(Id, Name, Description) values (1, 'Application',NULL);
INSERT INTO EventType(Id, Name, Description) values (2, 'Fault',NULL);
INSERT INTO EventType(Id, Name, Description) values (3, 'IO',NULL);
INSERT INTO Command (CommandId, Title, Description) values(1,'Shutdown','Quits the IrrigationController executable');
-- end attached script 'script'
