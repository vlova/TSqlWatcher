# TSqlWatcher

This tool is used to instantly update your changes in your stored procedures, functions or views.
Currently schemabound functions & views & update of custom type aren't supported, cuz update of them requires recursive deleting & recreating of dependent code.

Usage: 

    TSqlWatcher --path "D:\dev\myProject\schema\dbo\" --connectionString "Data Source=localhost\sqlexpress;Initial Catalog=MyProject;Integrated Security=True;Pooling=False;MultipleActiveResultSets=True
