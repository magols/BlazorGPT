

workspace {
    model {
        bridgeSystem = softwareSystem "Storm Elevate Bridge" "Schedules exports. Converts data" {
            azureStorage = container "Azure Blob Storage" "Downloads and processed files"
            azureTables = container "Azure TableStorage" "Local cahced data, state and logs" {
            }

            StormTriggerExportOrchestrator = container "StormTriggerExportOrchestrator" "Azure Functions durable task Orchestrator Function" {
                stormTriggerExportWorker = component "StormTriggerExportWorker" "Performs export tasks"
            }

            azureLoadBalancer = container "Azure Load Balancer" "Distributes traffic across multiple instances of StormTriggerExportOrchestrator" {
            }

            azureTables -> stormTriggerExportWorker "Reads last export deltas"
            stormTriggerExportWorker -> azureTables "Writes last export deltas"
            stormTriggerExportWorker -> stormSystem "Makes HTTP GET requests to trigger exports"
            stormTriggerExportWorker -> stormSystem "Makes HTTP GET requests to see if export is ready"
            stormTriggerExportWorker -> elevateSystem "Makes HTTP POST to upload data"
        }

        stormSystem = softwareSystem "Storm" "E-commerce platform" {
            stormStorage = container "Storm Storage" "Stores export JSON files to blob storage"
            stormChannel = container "Storm Channel" "Exports product and productstatus data"
            stormLoadBalancer = container "Storm Load Balancer" "Distributes traffic across multiple instances of StormChannel"

            stormChannel -> stormStorage "Stores export JSON files"
            StormTriggerExportOrchestrator -> azureLoadBalancer "Routes traffic to worker instances"
            azureLoadBalancer -> StormTriggerExportOrchestrator "Routes traffic from load balancer to worker instances"
            stormChannel -> stormLoadBalancer "Routes traffic to multiple instances of StormChannel"
        }

        elevateSystem = softwareSystem "Voyado Elevate 4" "CRM and loyalty platform"
        securitySystem = softwareSystem "Security System" "Responsible for securing the system"

        azureTables -> StormTriggerExportOrchestrator "Shares cached data and logs"
        stormTriggerExportWorker -> securitySystem "Implements security protocols for export tasks"
        stormChannel -> securitySystem "Implements security protocols for export data"
        elevateSystem -> securitySystem "Implements security protocols for upload data"
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

        container elevateSystem "ContainersElevate"{
            include *
            autolayout lr
        }

        container securitySystem "Security" {
            include *
            autolayout lr
        }

        theme default
    }
}

