
workspace {
    model {
        admin = person "Admin" "An administrator of my software system."
        softwareSystem = softwareSystem "Software System" "My software system."

        admin -> softwareSystem "Uses"
    }

    views {
        systemContext softwareSystem "SystemContext" {
            include *
            autoLayout
        }

        styles {
            element "Software System" {
                background #1168bd
                color #ffffff
            }
            element "Person" {
                shape person
                background #08427b
                color #ffffff
            }
        }
    }
}
