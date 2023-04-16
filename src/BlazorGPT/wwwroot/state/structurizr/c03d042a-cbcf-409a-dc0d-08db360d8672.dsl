 for the initial DSL model that was provided:

```
[STATEDATA]
workspace {
  model {
    bridgeSystem = softwareSystem "Storm Elevate Bridge" "Schedules exports. Converts data." {
        azureStorage = container "Azure Blob Storage" "Downloaded and processed files"
        azureTables = container "Azure TableStorage" "Local data, state and logs" {
        }
        functionApp = container "Azure Functions App" "Azure Functions" {
         
            StormExportOrchestrator = component "StormExportOrchestrator" "Azure Functions durable task Orchestrator Function" {
            }
		    
            TriggerFunctions = component "Timer Trigger Functions" "Triggers the orchestrator to run. For market/exporttype." {
                TriggerFunctions -> StormExportOrchestrator "Triggers"
            }


            StormCheckStatusAndDownloadFile = component "StormCheckStatusAndDownloadFile" "Checks export job status and downloads file when ready" {
            }

            ProductFilter = component "ProductFilter" "Filters products based on Status and Flags" {
		    }

            ProductStatusConverter = component "ProductStatusConverter" "Converts product status to Elevate format" {
            }

            ProcessDownloadedProductFiles = component "ProcessDownloadedProductFiles" "Processes downloaded product files" {
		    }
          
            ProcessDownloadedProductStatusFiles = component "ProcessDownloadedProductStatusFiles" "Processes downloaded product status files" {
            }

            FullExportMergeFunction = component "FullExportMergeFunction" "Merges full exports for all 3 markets into one gzip" {
		    }

            elevateImport = component "elevate-import" "Queue for uploads"{
            }

            ElevateImportQueueListener = component "ElevateImportQueueListener" "Listens for message on queue" {
            }
                            
            Upload = component "Upload" "Import sequence for Elevate" {
		    }

            PostFileToElevate = component "PostFileToElevate" "Posts file to Elevate import endpoint"

            ElevateCheckStatus = component "ElevateCheckStatus" "Tracks the status for the Elevate import"
        }

        timerTriggers = container "Timer Triggers" "Triggers the orchestrator to run" {
		    
            seDeltaProducts = component "SE_Delta_Products" "Triggers the Export orchestrator to export"
            seFullProducts = component "SE_Full_Products" "Triggers the Export orchestrator to export"
            seDeltaProductStatus = component "SE_Delta_ProductStatus" "Triggers the Export orchestrator to export"
            seFullProductStatus = component "SE_Full_ProductStatus" "Triggers the Export orchestrator to export"
        
            
            noDeltaProducts = component "NO_Delta_Products" "Triggers the Export orchestrator to export"
            noFullProducts = component "NO_Full_Products" "Triggers the Export orchestrator to export"
            noDeltaProductStatus = component "NO_Delta_ProductStatus" "Triggers the Export orchestrator to export"
            noFullProductStatus = component NO_Full_ProductStatus" "Triggers the Export orchestrator to export"
        
            
           fiDeltaProducts = component "FI_Delta_Products" "Triggers the Export orchestrator to export"
           fiFullProducts = component "FI_Full_Products" "Triggers the Export orchestrator to export"
           fiDeltaProductStatus = component "FI_Delta_ProductStatus" "Triggers the Export orchestrator to export"
           fiFullProductStatus = component "FI_Full_ProductStatus" "Triggers the Export orchestrator to export"
        }
    }

    stormSystem = softwareSystem "Storm" "E-commerce platform" {
        stormStorage = container "Azure Blob Storage" ""
        stormChannel = container "Storm Channel" "Exports product and product status data"
        stormChannel -> stormStorage "Stores export JSON files"
        StormExportOrchestrator -> azureStorage "Downloads exported JSON files"
    }

    elevateSystem = softwareSystem "Voyado Elevate 4" "CRM and loyalty platform" 

    ProcessDownloadedProductFiles -> ProductFilter "Uses"
    ProcessDownloadedProductStatusFiles -> ProductStatusConverter "Uses"

    ProcessDownloadedProductFiles -> azureTables "Writes last export deltas"
    ProcessDownloadedProductStatusFiles -> azureTables "Writes last export deltas"

    ProcessDownloadedProductFiles -> azureStorage "Reads export blob"
    ProcessDownloadedProductStatusFiles -> azureStorage "Reads export blob"

    StormExportOrchestrator -> azureTables "Reads last export deltas"
    

    StormExportOrchestrator -> stormSystem "Makes HTTP GET requests to trigger exports"
    StormExportOrchestrator -> stormSystem "Makes HTTP GET requests to see if export is ready"
    StormExportOrchestrator -> elevateSystem "Makes HTTP POST to upload data"
    StormExportOrchestrator -> elevateSystem "Checks if export is successful"


  }

  views {
    systemContext bridgeSystem "SystemContext" {
        include *
        autolayout lr
    }

    container bridgeSystem "Containers" {
        include *
        autolayout lr
    }

    component functionApp "Components" {
		include *
		autolayout
	}

    component timerTriggers "timerTriggers" {
        include *
        
    }

    container stormSystem "ContainersStorm"{
    	include *
    	autolayout lr
    }


    theme default
  }
}
