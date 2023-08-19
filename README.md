# BlazorGPT

A Blazor Server chat application that uses the GPT endpoints available from OpenAI and MS Azure OpenAI. 

- It allows you to create and manage multiple chat sessions with history. You can create QuickProfiles for quick access to your favorite text snippet shortcuts. 

- Create Scripts with mutiple steps for automating a conversation. 
- Branch conversations into side conversations with the same context. 
- Restart a conversation from a previous step. 
- Customize the chat experience with middlewares and filters.

As a developer it's extensible and you can write your own plugins in the form of IInterceptors that intercepts the chat messages and modify them before they are sent to the API. 

AI support is based on <a href="https://learn.microsoft.com/en-us/semantic-kernel/overview">Semantic Kernel</a>, a project that adds support for API:s with concepts such as planners and support for Bing Copilots and OpenAI plugins.


[GitHub](https://github.com/magols/BlazorGPT)

[Installation instructions](docs/setup.md)


## Features
- Chat with GPT-3.5 or GPT-4
  ![](docs/images/chat.png)
  
- Create and manage multiple chat sessions with history
  ![](docs/images/history.png)
  
- QuickProfiles for quick access to your favorite text snippet shortcuts
  ![](docs/images/QP.png)

- Scripts with mutiple steps for automating a conversation
  ![](docs/images/editscript.png)
  
- Branching of conversations into side conversations with the same context
  ![](docs/images/hasbranch.png)
  ![](docs/images/branched.png)

- Restart a conversation from a previous step

[Installation instructions](docs/setup.md)
