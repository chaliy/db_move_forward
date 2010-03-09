This is database migration tool inspired by Ruby on Rails Active Record Migrations.

# Specification #
  
##  Upping Scenario ##

1. Check if database has tables with name "__MigrationVesions" 
   and "__MigrationLog" if not exit and propose to init ( this
   behaviour is fool protection if database name is 
   incorrect )
2. Get version for migration name (migration name is ability 
   to use few migration sequences in single database, 
   however, I am not sure if this is not overcomlication)
   Possible that this stuff also need fool prof.
3. Ensure all steps after last version.
4. Execute discovered steps if any. Each step executed separatelly
   and advances version according to the executed step.
5. If something failed, rollback current step

##  Info Scenario  ##

1. Should check "__Versions" table and current version for migration.
   Display all available information.
2. If config is correct should find all penfing steps. And display
   all applicable information.
3. Possible show last migration date.

## Init Scenario ##

1. Should check and create tables "__MigrationVesions" 
   and "__MigrationLog" (if not exists)
2. Should add record to __MigrationVesions with migration
   sequence name and version 0

##  General considerations  ##

1. Steps should be indempotent as much as possible
2. Steps should implement rollback (or at least try to do so)
3. Rollback should be designed in exception less maner.
	For example AddTable should not fail if table does not exist
