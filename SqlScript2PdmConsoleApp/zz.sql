
create table Employer(
    ID          serial                                                                         not null,  --id
    Title       varchar(100)                                                                   null,      --
    Remark      text                                                                           null,      --
    UpdateTime  timestamp                                                                      null,      --
    UpdateUser  int                     default (0)                                            null,      --
    CreateTime  timestamp               default now()                                          null,      -- createtime 
    primary key (ID)
);

drop table if exists ShortApp ;
create table ShortApp(
    Tel   varchar(20)                                                     not null,  --
    IsApp int            default (0)                                      not null   --
);
drop table if exists MarketProduct ;
create table MarketProduct(
    fday       date                                                         null,      --
    spreadname varchar(50)                                                  null,      --
    fcount     int                                                          null       --
);