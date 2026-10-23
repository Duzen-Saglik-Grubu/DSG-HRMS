-- LOGO Bordro veritabaninin TEST kopyasi: yalnizca HRMS'in okudugu tablolar ve
-- kolonlar, canli veritabanindaki veri tipleriyle (26.09.2026, INFORMATION_SCHEMA).
-- Canli tablolarda cok daha fazla kolon vardir; birkac tanesi, senkronizasyonun
-- fazladan kolonlardan etkilenmedigini gostermek icin eklenmistir.
--
-- ICINDEKI VERI SENTETIKTIR (KVKK). Gercek kisilere ait deger yoktur.

CREATE TABLE LH_001_PERSON (
    LREF       int          NOT NULL PRIMARY KEY,
    CODE       varchar(17)  NULL,
    TTFNO      varchar(21)  NULL,
    NAME       varchar(21)  NULL,
    SURNAME    varchar(21)  NULL,
    BIRTHDATE  datetime     NULL,
    INDATE     datetime     NULL,
    OUTDATE    datetime     NULL,
    FIRMNR     smallint     NULL,
    SEX        smallint     NULL,
    EDUCATION  smallint     NULL,
    TITLE      varchar(41)  NULL,
    SPECODE    varchar(25)  NULL,
    JOBREF     int          NULL,
    LOCNR      smallint     NULL,
    DEPTNR     smallint     NULL
);

CREATE TABLE LH_001_CONTACT (
    LREF     int          NOT NULL PRIMARY KEY,
    CARDREF  int          NULL,
    TYP      smallint     NULL,
    EXP1     varchar(61)  NULL,
    EXP2     varchar(61)  NULL
);

CREATE TABLE L_CAPIFIRM (
    NR    smallint     NULL,
    NAME  varchar(61)  NULL,
    TITLE varchar(201) NULL
);

-- T1 tablolari (23.10.2026, INFORMATION_SCHEMA). LDATA ve BLOODGROUP gibi kolonlar,
-- senkronizasyonun bunlari OKUMADIGINI gostermek icin vardir.
CREATE TABLE LH_001_FAMILY (
    LREF      int          NOT NULL PRIMARY KEY,
    PERREF    int          NULL,
    IDREF     int          NULL,
    RELATION  smallint     NULL,
    STATUS    smallint     NULL,
    NAME      varchar(21)  NULL,
    LDATA     image        NULL
);

CREATE TABLE LH_001_PERIDINF (
    LREF        int          NOT NULL PRIMARY KEY,
    EXSURNAME   varchar(21)  NULL,
    BLOODGROUP  smallint     NULL,
    RELIGION    varchar(21)  NULL
);

CREATE TABLE LH_001_TYPEDEF (
    LREF   int          NOT NULL PRIMARY KEY,
    TYP    smallint     NULL,
    NR     smallint     NULL,
    EXP    varchar(61)  NULL,
    DESC_  varchar(61)  NULL
);

CREATE TABLE L_CAPIDIV (
    LOGICALREF  int          NOT NULL PRIMARY KEY,
    FIRMNR      smallint     NULL,
    NR          smallint     NULL,
    NAME        varchar(61)  NULL,
    CITY        varchar(21)  NULL
);

CREATE TABLE L_CAPIDEPT (
    LOGICALREF  int          NOT NULL PRIMARY KEY,
    FIRMNR      smallint     NULL,
    NR          smallint     NULL,
    NAME        varchar(61)  NULL
);
