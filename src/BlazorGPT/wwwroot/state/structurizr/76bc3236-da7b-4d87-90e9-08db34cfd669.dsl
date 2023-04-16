

workspace { 
    model { 
        bridgeSystem = softwareSystem "Storm Elevate Bridge" "Schedules exports. Converts data" {
            azureStorage = container "Azure Blob Storage" "Downloaded and processed files"
            azureTables = container "Azure TableStorage" "Local data, state and logs" {
            }

            StormTriggerExportOrchestrator = container "StormTriggerExportOrchestrator" "Azure Functions durable task Orchestrator Function" {

            }
        }

        stormSystem = softwareSystem "Storm" "E-commerce platform" {
            stormStorage = container "Storm Storage" "Stores export JSON files to blob storage"
            stormChannel = container "Storm Channel" "Exports product and productstatus data"

            stormChannel -> stormStorage "Stores export JSON files"
            StormTriggerExportOrchestrator -> azureStorage "Downloads exported JSON files"
        }

        elevateSystem = softwareSystem "Voyado Elevate 4" "CRM and loyalty platform"

        azureTables -> StormTriggerExportOrchestrator "Reads last export deltas"
        StormTriggerExportOrchestrator -> azureTables "Writes last export deltas"
        StormTriggerExportOrchestrator -> stormSystem "Makes HTTP GET requests to trigger exports"
        StormTriggerExportOrchestrator -> stormSystem "Makes HTTP GET requests to see if export is ready"
        StormTriggerExportOrchestrator -> elevateSystem "Makes HTTP POST to upload data"

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

        container stormSystem "ContainersStorm"{
			include *
			autolayout lr
		}

       theme default

    }
}


