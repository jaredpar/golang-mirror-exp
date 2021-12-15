Go package mirror experiment
====

This is an experiment to see if it is possibly to back a `%GOPROXY%` using Azure Blob Storage.

The result was a success which shouldn't come as too much of a surprise. As long as the blob names are setup with path like namse then it mirrors a file system for which `%GOPROXY%` is designed to work.
