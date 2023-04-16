using System.Text;
using BlazorGPT.Data;
using Microsoft.EntityFrameworkCore;

namespace BlazorGPT.Managers;

public class StructurizrDslInterceptor : InterceptorBase, IInterceptor, IStateWritingInterceptor
{

    private readonly ConversationsRepository _conversationsRepository;
    private readonly IDbContextFactory<BlazorGptDBContext> _context;

    public StructurizrDslInterceptor(IDbContextFactory<BlazorGptDBContext> context, ConversationsRepository conversationsRepository) : base(context, conversationsRepository)
    {
        _conversationsRepository = conversationsRepository;
        _context = context;
    }
    

    public async Task<Conversation> Send(Conversation conversation)
    {


        if (conversation.Messages.Count() == 2)
        {
            await AppendInstruction(conversation);
        }

        return conversation;
    }

    private async Task<Conversation> AppendInstruction(Conversation conversation)
    {

        //string state = await File.ReadAllTextAsync(path + "structurizr\\workspace.dsl");

        string state = @"

        

workspace { 
    model { 
        bridgeSystem = softwareSystem ""Storm Elevate Bridge"" ""Schedules exports. Converts data."" {
            azureStorage = container ""Azure Blob Storage"" ""Downloaded and processed files""
            azureTables = container ""Azure TableStorage"" ""Local data, state and logs"" {
            }
            functionApp = container ""Azure Functions App"" ""Azure Functions"" {
             

             
                StormExportOrchestrator = component ""StormExportOrchestrator"" ""Azure Functions durable task Orchestrator Function"" {
                }
			    
                TriggerFunctions = component ""Timer Trigger Functions"" ""Triggers the orchestrator to run. For market/exporttype."" {
                    TriggerFunctions -> StormExportOrchestrator ""Triggers""
				}


                StormCheckStatusAndDownloadFile = component ""StormCheckStatusAndDownloadFile"" ""Checks export job status and downloads file when ready"" {
                }

                ProductFilter = component ""ProductFilter"" ""Filters products based on Status and Flags"" {
				}

                ProductStatusConverter = component ""ProductStatusConverter"" ""Converts product status to Elevate format"" {
                }

                ProcessDownloadedProductFiles = component ""ProcessDownloadedProductFiles"" ""Processes downloaded product files"" {
				}
              
                ProcessDownloadedProductStatusFiles = component ""ProcessDownloadedProductStatusFiles"" ""Processes downloaded product status files"" {
                }

                FullExportMergeFunction = component ""FullExportMergeFunction"" ""Merges full exports for all 3 markets into one gzip"" {
				}

                elevateImport = component ""elevate-import"" ""Queue for uploads""{
                }

                ElevateImportQueueListener = component ""ElevateImportQueueListener"" ""Listens for message on queue"" {
                }
                                
                Upload = component ""Upload"" ""Import sequence for Elevate"" {
				}

                PostFileToElevate = component ""PostFileToElevate"" ""Posts file to Elevate import endpoint""

                ElevateCheckStatus = component ""ElevateCheckStatus"" ""Tracks the status for the Elevate import""
            }

            timerTriggers = container ""Timer Triggers"" ""Triggers the orchestrator to run"" {
			    
                seDeltaProducts = component ""SE_Delta_Products"" ""Triggers the Export orchestrator to export""
                seFullProducts = component ""SE_Full_Products"" ""Triggers the Export orchestrator to export""
                seDeltaProductStatus = component ""SE_Delta_ProductStatus"" ""Triggers the Export orchestrator to export""
                seFullProductStatus = component ""SE_Full_ProductStatus"" ""Triggers the Export orchestrator to export""
            
                
                noDeltaProducts = component ""NO_Delta_Products"" ""Triggers the Export orchestrator to export""
                noFullProducts = component ""NO_Full_Products"" ""Triggers the Export orchestrator to export""
                noDeltaProductStatus = component ""NO_Delta_ProductStatus"" ""Triggers the Export orchestrator to export""
                noFullProductStatus = component NO_Full_ProductStatus"" ""Triggers the Export orchestrator to export""
            
                
               fiDeltaProducts = component ""FI_Delta_Products"" ""Triggers the Export orchestrator to export""
               fiFullProducts = component ""FI_Full_Products"" ""Triggers the Export orchestrator to export""
               fiDeltaProductStatus = component ""FI_Delta_ProductStatus"" ""Triggers the Export orchestrator to export""
               fiFullProductStatus = component ""FI_Full_ProductStatus"" ""Triggers the Export orchestrator to export""
            }




        }

        stormSystem = softwareSystem ""Storm"" ""E-commerce platform"" {
            stormStorage = container ""Azure Blob Storage"" """"
            stormChannel = container ""Storm Channel"" ""Exports product and product status data""
            stormChannel -> stormStorage ""Stores export JSON files""
            StormExportOrchestrator -> azureStorage ""Downloads exported JSON files""
        }

        elevateSystem = softwareSystem ""Voyado Elevate 4"" ""CRM and loyalty platform"" 




        ProcessDownloadedProductFiles -> ProductFilter ""Uses""
        ProcessDownloadedProductStatusFiles -> ProductStatusConverter ""Uses""

        ProcessDownloadedProductFiles -> azureTables ""Writes last export deltas""
        ProcessDownloadedProductStatusFiles -> azureTables ""Writes last export deltas""

        ProcessDownloadedProductFiles -> azureStorage ""Reads export blob""
        ProcessDownloadedProductStatusFiles -> azureStorage ""Reads export blob""

        StormExportOrchestrator -> azureTables ""Reads last export deltas""
        

        StormExportOrchestrator -> stormSystem ""Makes HTTP GET requests to trigger exports""
        StormExportOrchestrator -> stormSystem ""Makes HTTP GET requests to see if export is ready""
        StormExportOrchestrator -> elevateSystem ""Makes HTTP POST to upload data""
        StormExportOrchestrator -> elevateSystem ""Checks if export is successful""


    }

    views {
        systemContext bridgeSystem ""SystemContext"" {
            include *
            autolayout lr
        }

        container bridgeSystem ""Containers"" {
            include *
            autolayout lr
        }

        component functionApp ""Components"" {
			include *
			autolayout
		}

        component timerTriggers ""timerTriggers"" {
            include *
            
        }

        container stormSystem ""ContainersStorm""{
        	include *
        	autolayout lr
        }


        theme default
    }
}
 

        ";

        //        string state = @"

        //workspace {

        //    model {
        //        user = person ""User"" ""A user of my software system.""
        //        softwareSystem = softwareSystem ""Software System"" ""My software system.""

        //        user -> softwareSystem ""Uses""
        //    }

        //    views {
        //        systemContext softwareSystem ""SystemContext"" {
        //            include *
        //            autoLayout
        //        }

        //        styles {
        //            element ""Software System"" {
        //                background #1168bd
        //                color #ffffff
        //            }
        //            element ""Person"" {
        //                shape person
        //                background #08427b
        //                color #ffffff
        //            }
        //        }
        //    }

        //}
        //";


        var contentBuilder = new StringBuilder();
        contentBuilder.Append("[INSTRUCTION]Here is the data for an initial DSL model. We will iteratively add data to this data on our respective side. If either one has changes the structure or values of the model we shall send over the model and data enclosed in a [STATEDATA][/STATEDATA] tag in our message. In the STATEDATA tag nothing else should appear.  We will send it to eachother ONLY when data has changed! ");
        contentBuilder.Append(" Here is the base data. Remember from the start not to send it back unless you have changed structure or values.");
        contentBuilder.Append($" [STATEDATA]{state}[/STATEDATA][/INSTRUCTION]");

        var outgoing = conversation.Messages.ElementAt(1).Content;
        contentBuilder.AppendLine(outgoing);
        conversation.Messages.ElementAt(1).Content = contentBuilder.ToString();

        conversation.Messages.ElementAt(1).State = new MessageState()
        {
            Type = "StructurizrDslInterceptor",
            Content = state,
            IsPublished = true,
            Name = "msgstate",
        };

        return conversation;
    }   

    private string path = @"C:\source\BlazorGPT\BlazorGPT\wwwroot\state\";


    public bool Internal { get; } = false;

    public async Task<Conversation> Receive(Conversation conversation)
    {
        var lastMsg = conversation.Messages.Last();
        await ParseMessageAndSaveState(lastMsg, "StructurizrDslInterceptor");

        if (File.Exists(path + lastMsg.Id))
        {
            File.Move(path + lastMsg.Id, path + "structurizr\\" + lastMsg.Id + ".dsl");
            File.Copy(
                path + "structurizr\\" + lastMsg.Id + ".dsl", path + "structurizr\\workspace.dsl", true);

        }

        return conversation;
    }


    public string Name { get; } = "Structurizr Hive DSL";

    public static string DecodeStringFromCSharp(string input)
    {
        return input
            .Replace("\\\\", "\\")
            .Replace("\\\"", "\"")
            .Replace("\\\'", "\'")
            .Replace("\\n", "")
            .Replace("\\r", "");
    }

    public static string EscapeStringForCSharp(string input)
    {
        return input
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\'", "\\\'")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}