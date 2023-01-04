create or replace function update_db_model() returns void as 
$$ 
begin 
 
 ------------------------------------------------------------------------------------------------
 --
    if not exists(select * from information_schema.tables  
        where  
            table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
            and table_name = 'recipe_cell_access_permission_whitelist') then 
 
			CREATE TABLE "recipe_cell_access_permission_whitelist"
			(
				"uid" text NOT NULL,
				"recipeName" text NOT NULL,
				"stepUid" text NOT NULL,
				"columnName" text NOT NULL,
				"whoSet" text,
				"whenSet" timestamp without time zone,
				CONSTRAINT "recipe_cell_access_permission_whitelist_pkey" PRIMARY KEY ("uid")
			)
			WITH (
			OIDS=FALSE
			);
			ALTER TABLE "recipe_cell_access_permission_whitelist"
			OWNER TO postgres;
			GRANT SELECT ON TABLE "recipe_cell_access_permission_whitelist" TO postgres; 
    end if; 


 ------------------------------------------------------------------------------------------------
 --
    if not exists(select * from information_schema.tables  
        where  
            table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
            and table_name = 'carrier_data') then 
 
			CREATE TABLE "carrier_data"
			(
				"guid" text NOT NULL,
				"load_time" timestamp without time zone,
				"unload_time" timestamp without time zone,
				"rfid" text,
				"lot_id" text,
				"product_category" text,
				"station" text,
				CONSTRAINT "carrier_data_pkey" PRIMARY KEY ("guid" )
			)
			WITH (
			OIDS=FALSE
			);
			ALTER TABLE "carrier_data"
			OWNER TO postgres;
			GRANT SELECT ON TABLE "carrier_data" TO postgres; 
    end if; 

 ------------------------------------------------------------------------------------------------
 --
	if not exists(select * from information_schema.tables  
        where  
            table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
            and table_name = 'wafer_data') then 
 
			CREATE TABLE "wafer_data"
			(
				"guid" text NOT NULL,
				"create_time" timestamp without time zone,
				"delete_time" timestamp without time zone,
				"carrier_data_guid" text,
				"create_station" text,
				"create_slot" text,
				"process_data_guid" text,
				"wafer_id" text,
				"lasermarker1" text,
				"lasermarker2" text,
				"lasermarker3" text,
				"t7code1" text,
				"t7code2" text,
				"t7code3" text,
				"pj_data_guid" text,
				"lot_data_guid" text,
				"lot_id" text,
				"notch_angle" real,
				"sequence_name" text,
				"process_status" text,
				CONSTRAINT "wafer_data_pkey" PRIMARY KEY ("guid" )
			)
			WITH (
			OIDS=FALSE
			);
			ALTER TABLE "wafer_data"
			OWNER TO postgres;
			GRANT SELECT ON TABLE "wafer_data" TO postgres; 
    end if; 
 ------------------------------------------------------------------------------------------------
 --	
	 if not exists(select * from information_schema.tables  
        where  
            table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
            and table_name = 'event_data') then 
 
					CREATE TABLE "event_data"
				(
				  "gid" serial NOT NULL,
				  "event_id" integer,
				  "event_enum" text,
				  "type" text,
				  "source" text,
				  "description" text,
				  "level" text,
				  "occur_time" timestamp without time zone,
				  CONSTRAINT "event_data_pkey" PRIMARY KEY ("gid" )
				)
					WITH (
					OIDS=FALSE
					);
					--ALTER TABLE "EventManager"
					--OWNER TO postgres;
					GRANT ALL ON TABLE "event_data" TO postgres;
					GRANT SELECT ON TABLE "event_data" TO postgres;

					CREATE INDEX "event_data_occur_time_event_id_idx"
					ON "event_data"
					USING btree
					("occur_time" , "event_id" );
    end if; 

 ------------------------------------------------------------------------------------------------
 --	
	    if not exists(select * from information_schema.tables  
				where  
				table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
				and table_name = 'wafer_move_history') then 
 
					CREATE TABLE "wafer_move_history"
					(
					  "gid" serial NOT NULL,
					  "wafer_data_guid" text,
					  "arrive_time" timestamp without time zone,
					  "station" text,
					  "slot" text,
					  "status" text,
					  CONSTRAINT "wafer_move_history_pkey" PRIMARY KEY ("gid" )
					)
					WITH (
					  OIDS=FALSE
					);
					ALTER TABLE "wafer_move_history"
					OWNER TO postgres;
					GRANT SELECT ON TABLE "wafer_move_history" TO postgres;
 
    end if; 
 ------------------------------------------------------------------------------------------------
 --	
	    if not exists(select * from information_schema.tables  
				where  
				table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
				and table_name = 'process_data') then 
 
					CREATE TABLE "process_data"
					(
					  "guid" text NOT NULL,
					  "process_begin_time" timestamp without time zone,
					  "process_end_time" timestamp without time zone,
					  "recipe_name" text,
					  "process_status" text,
					   "wafer_data_guid" text,
					   "process_in" text,
					  CONSTRAINT "process_data_pkey" PRIMARY KEY ("guid" )
					)
					WITH (
					  OIDS=FALSE
					);
					ALTER TABLE "process_data"
					OWNER TO postgres;
					GRANT SELECT ON TABLE "process_data" TO postgres;
 
    end if; 

 ------------------------------------------------------------------------------------------------
 --	
	    if not exists(select * from information_schema.tables  
				where  
				table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
				and table_name = 'stats_data') then 
 
					CREATE TABLE "stats_data"
					(
					  "name" text,
					  "value" integer,
					  "total" integer,
					  "description" text,
					  "last_update_time" timestamp without time zone,
					  "last_reset_time" timestamp without time zone,
					  "last_total_reset_time" timestamp without time zone,		
					  "is_visible" boolean,
					  "enable_alarm" boolean,
					  "alarm_value" integer,
					  CONSTRAINT "stats_data_pkey" PRIMARY KEY ("name" )
					)
					WITH (
					  OIDS=FALSE
					);
					ALTER TABLE "stats_data"
					OWNER TO postgres;
					GRANT SELECT ON TABLE "stats_data" TO postgres;
 
    end if; 
	
	------------------------------------------------------------------------------------------------
 --	
	    if not exists(select * from information_schema.tables  
				where  
				table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
				and table_name = 'PM_statsData') then 
 
					CREATE TABLE "PM_statsData"
					(
					  "chamber" text,
					  "name" text,
					  "partsNo" text,
					  "current_cycle" text,
					  "target_cycle" text,
					  "current_value" text,
					  "warning_value" text,
					  "target_value" text,
					  "install_Date" timestamp without time zone,
					  CONSTRAINT "PM_statsData_pkey" PRIMARY KEY ("name" )
					)
					WITH (
					  OIDS=FALSE
					);
					ALTER TABLE "PM_statsData"
					OWNER TO postgres;
					GRANT SELECT ON TABLE "PM_statsData" TO postgres;
 
    end if; 
	
	------------------------------------------------------------------------------------------------
 --	
	    if not exists(select * from information_schema.tables  
				where  
				table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
				and table_name = 'PM_statsHistory') then 
 
					CREATE TABLE "PM_statsHistory"
					(
					  "id" serial,
					  "chamber" text,
					  "name" text,
					  "partsNo" text,
					  "current_cycle" text,
					  "target_cycle" text,
					  "current_value" text,
					  "warning_value" text,
					  "target_value" text,
					  "install_Date" timestamp without time zone,
					  CONSTRAINT "PM_statsHistory_pkey" PRIMARY KEY ("id" )
					)
					WITH (
					  OIDS=FALSE
					);
					ALTER TABLE "PM_statsHistory"
					OWNER TO postgres;
					GRANT SELECT ON TABLE "PM_statsHistory" TO postgres;
 
    end if; 

------------------------------------------------------------------------------------------------
--	
	    if not exists(select * from information_schema.tables  
				where  
				table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
				and table_name = 'leak_check_data') then 
 
					CREATE TABLE "leak_check_data"
					(
						"guid" text NOT NULL,
						"operate_time" timestamp without time zone,
						"status" text,
						"leak_rate" real,
						"start_pressure" real,
						"stop_pressure" real,
						"mode" text,
						"leak_check_time" integer,
					  CONSTRAINT "leak_check_data_pkey" PRIMARY KEY ("guid" )
					)
					WITH (
					  OIDS=FALSE
					);
					ALTER TABLE "leak_check_data"
					OWNER TO postgres;
					GRANT SELECT ON TABLE "leak_check_data" TO postgres;
 
    end if; 
 ------------------------------------------------------------------------------------------------
 --
    if not exists(select * from information_schema.tables  
        where  
            table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
            and table_name = 'cj_data') then 
 
			CREATE TABLE cj_data
			(
				"guid" text NOT NULL,
				"start_time" timestamp without time zone,
				"end_time" timestamp without time zone,
				"carrier_data_guid" text,
				"name" text,
				"input_port" text,
				"output_port" text,
				"total_wafer_count" integer,
				"abort_wafer_count" integer,
				"unprocessed_wafer_count" integer,
				CONSTRAINT "cj_data_pkey" PRIMARY KEY ("guid" )
			)
			WITH (
			OIDS=FALSE
			);
			ALTER TABLE "cj_data"
			OWNER TO postgres;
			GRANT SELECT ON TABLE "cj_data" TO postgres; 
    end if; 
 ------------------------------------------------------------------------------------------------
 --
    if not exists(select * from information_schema.tables  
        where  
            table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
            and table_name = 'pj_data') then 
 
			CREATE TABLE pj_data
			(
				"guid" text NOT NULL,
				"start_time" timestamp without time zone,
				"end_time" timestamp without time zone,
				"carrier_data_guid" text,
				"cj_data_guid" text,
				"name" text,
				"input_port" text,
				"output_port" text,
				"total_wafer_count" integer,
				"abort_wafer_count" integer,
				"unprocessed_wafer_count" integer,
				CONSTRAINT "pj_data_pkey" PRIMARY KEY ("guid" )
			)
			WITH (
			OIDS=FALSE
			);
			ALTER TABLE "pj_data"
			OWNER TO postgres;
			GRANT SELECT ON TABLE "pj_data" TO postgres; 
    end if; 
 ------------------------------------------------------------------------------------------------
 --	
    if not exists(select * from information_schema.tables  
        where  
            table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
            and table_name = 'lot_data') then 
 
			CREATE TABLE lot_data
			(
				"guid" text NOT NULL,
				"start_time" timestamp without time zone,
				"end_time" timestamp without time zone,
				"carrier_data_guid" text,
				"cj_data_guid" text,
				"name" text,
				"input_port" text,
				"output_port" text,
				"total_wafer_count" integer,
				"abort_wafer_count" integer,
				"unprocessed_wafer_count" integer,
				CONSTRAINT "lot_data_pkey" PRIMARY KEY ("guid" )
			)
			WITH (
			OIDS=FALSE
			);
			ALTER TABLE "lot_data"
			OWNER TO postgres;
			GRANT SELECT ON TABLE "lot_data" TO postgres; 
    end if; 

		 ------------------------------------------------------------------------------------------------
 --
    if not exists(select * from information_schema.tables  
        where  
            table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
            and table_name = 'lot_wafer_data') then 
 
			CREATE TABLE lot_wafer_data
			(
				"guid" text NOT NULL,
				"create_time" timestamp without time zone,
				"lot_data_guid" text,
				"wafer_data_guid" text,
				CONSTRAINT "lot_wafer_data_pkey" PRIMARY KEY ("guid" )
			)
			WITH (
			OIDS=FALSE
			);
			ALTER TABLE "lot_wafer_data"
			OWNER TO postgres;
			GRANT SELECT ON TABLE "lot_wafer_data" TO postgres; 

			CREATE INDEX "lot_wafer_data_idx1"
			ON "lot_wafer_data"
			USING btree
			("lot_data_guid" , "wafer_data_guid" );
    end if; 


	 ------------------------------------------------------------------------------------------------
 --	
	    if not exists(select * from information_schema.tables  
				where  
				table_catalog = CURRENT_CATALOG and table_schema = CURRENT_SCHEMA 
				and table_name = 'recipe_step_data') then 
 
					CREATE TABLE "recipe_step_data"
					(
					  "guid" text NOT NULL,
					  "step_begin_time" timestamp without time zone,
					  "step_end_time" timestamp without time zone,
					  "step_name" text,
					  "step_time" real,
					   "process_data_guid" text,
					   "step_number" integer,
					  CONSTRAINT "recipe_step_data_pkey" PRIMARY KEY ("guid" )
					)
					WITH (
					  OIDS=FALSE
					);
					ALTER TABLE "recipe_step_data"
					OWNER TO postgres;
					GRANT SELECT ON TABLE "recipe_step_data" TO postgres;

								CREATE INDEX "recipe_step_data_idx1"
					ON "recipe_step_data"
					USING btree
					("process_data_guid" , "step_number" );
 
    end if; 




end; 
$$ 
language 'plpgsql'; 
 
select update_db_model(); 

CREATE OR REPLACE FUNCTION batch_delete_tables(text)
RETURNS int AS
$$
DECLARE
    r RECORD;
    count int;
BEGIN
    count := 0;
FOR r IN SELECT tablename FROM pg_tables where tablename like $1 || '%'  LOOP
    RAISE NOTICE 'tablename: %', r.tablename;
    EXECUTE 'DROP TABLE "' || r.tablename || '" CASCADE';
    count := count + 1;
END LOOP;
RETURN count;
END;
$$
LANGUAGE 'plpgsql' VOLATILE;
