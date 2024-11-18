
## Developing and using Semantic Kernel plugins

By using hot reload and the "Restart conversation" button in the chat, you can quickly develop and debug your plugins in a very close loop.

#### Creating native function plugins
1. Create a public .cs class file anywhere in the application. 

    If any of its methods are decorated with the ```[KernelFunction]``` attribute, the class will be registered as a plugin and its methods will be available as kernel functions.

    Note: The class must have a constructor that takes an ```IServiceProvider``` as a parameter. This is used so the plugins can resolve dependencies from the DI container.

2. Press F5 to run and debug the application. 

3. Make sure the Function calling interceptor is enabled in the interceptors selection list.

4. Enable your plugin in the plugins selection list.

Your native function plugin is now ready to use!

#### Creating semantic (text based) plugins
1. In the **src/BlazorGPT.Web/Plugins** folder, create a folder for your plugin. Underneath that folder, each function has its own subfolder with manifest and prompt file.

2. Make sure the Function calling interceptor is enabled in the interceptors selection list.

3. Enable your plugin in the plugins selection list.

Your semantic  function plugin is now ready to use!

## Reading external plugins from the Plugins folder
The application will automatically scan the Plugins folder for assemblies containing classes with methods decorated with the ```[KernelFunction]``` attribute. 

It will also scan for semantic plugins in the Plugins folder. 

This means that you can have a separate solution, repository or project for your plugins and just copy the compiled dlls and text files to the Plugins folder to use them. 

Please note that the application will currently not automatically load new plugins while running. You will need to restart the application to load new plugins.

See https://github.com/magols/BlazorGPT.Samples for such a sample plugin project. 
1. Clone this repository parallell to the BlazorGPT repo.  
2. Build the BlazorGPT.Samples project. It copies the compiled plugin dll and text files to the Plugins folder in the BlazorGPT.Web project.
3. Run the BlazorGPT.Web project.
4. Make sure the Function calling interceptor is enabled in the interceptors selection list.
5. Enable the plugins in the plugins selection list.

Your external function plugins are now ready to use!


