# ChatTheDoc
ChatTheDoc implments AI logic to make users interact to their pdf files and get information without opening and reading them - just by asking in a chat. 
The implmentation has a blazor based UI with python implmentation of the AI logic to interact with OpenAi's Chat GPT. 

## Required libraries to install 
* Python
Install python 3.11.5 from https://www.python.org/downloads/

* Dependencies
Install langchain and dependencies, 
``` pip install langchain openai pypdf chromadb tiktoken```

* .NET Install 
	Download and install .Net 7.0.
	- Install Runtime from https://dotnet.microsoft.com/en-us/download/dotnet/7.0/runtime?cid=getdotnetcore&os=windows&arch=x64
	- Install SDK https://download.visualstudio.microsoft.com/download/pr/a099e4b6-a6a8-4d34-bf95-b00739d35bb7/cdad50779717ba0e56caf89a3ba29ab1/dotnet-sdk-7.0.403-win-x64.exe
	- Install ASP.NET Core Hosting, if you host your site in IIS
	https://download.visualstudio.microsoft.com/download/pr/215095b0-dc0a-4e79-8815-3f72af83d054/3e7b7f99dffe2393a2210472c8c126a8/dotnet-hosting-7.0.13-win.exe

* Install Microsoft Visual C++ Microsoft Visual C++ 14.0 or greater is required. Get it with "Microsoft C++ Build Tools": https://visualstudio.microsoft.com/visual-cpp-build-tools/

- Select: Workloads → Desktop development with C++
- Select these Individual Components:
   - Windows 10 SDK
   - C++ x64/x86 build tools

## Usage
- Create a .env file with the following name\
     ```OPENAI_API_KEY=Your_OpenAI_API_Key ```
- Set envronment variable to your python dll e.g.\
     ```PYTHONNET_PYDLL  = "C:\Python\python39.dll" ```
- Copy your pdf documents to data/sample_pdfs directory.
- Run the tool (ChatTheDoc)
- Start chatting to the pdf by inserting your questions. ChatTheDoc will search through the pdf and gives you the desired solution

# Deployment
Deploying a Blazor project to another machine involves a series of steps. I'll guide you through the process step by step, including cleaning up unnecessary files.

### Step 1: Build the Blazor Server and Client Project

1. **Open a Command Prompt or Terminal**:
   - Navigate to the root directory of your Blazor project.

2. **Build the Project**:
   - Use the following command to build the project:
     ```
     dotnet build -c Release
     ```
   - This will compile the project in Release mode, optimizing it for deployment.

### Step 2: Publish the Blazor App

1. **Publish the App**:
   - Use the following command to publish the app:
     ```
     dotnet publish -c Release -o publish
     ```
   - This will publish the app to a folder named `publish`.

### Step 3: Prepare for Deployment

1. **Cleanup Unnecessary Files**:
   - Inside the `publish` folder, you can remove unnecessary files, such as `.pdb` (debug symbols) and `.runtimeconfig.json` files if they are present.

2. **Copy Required Files**:
   - Ensure you have the following files in the `publish` folder:
     - Your compiled application files.
     - The `wwwroot` folder (contains static content like CSS, JS, and images).
     - The `.dll` files.
     - The `.exe` file (if applicable).
     - The `.deps.json` and `.runtimeconfig.json` files (if applicable).

### Step 4: Transfer to Hosting Machine

1. **Transfer Files**:
   - Use a method like FTP, SCP, or any other file transfer protocol to move the `publish` folder to the target machine.

### Step 5: Set Up Hosting Environment

1. **Install .NET Runtime**:
   - Ensure that the .NET runtime matching your project version is installed on the hosting machine.

2. **Configure Web Server**:
   - If you're using a web server like IIS, set up a new site and point it to the folder containing your published Blazor app.

### Step 6: Start the Application

1. **Run the Application**:
   - Start the application using the appropriate method for the web server you're using.

### Optional Steps:

1. **Secure Your Application**:
   - If your application handles sensitive information, consider setting up HTTPS and other security measures.

2. **Monitoring and Maintenance**:
   - Set up monitoring and regular maintenance tasks to ensure your application runs smoothly.

Remember that these steps are a general guideline. Depending on your specific environment, you might encounter variations or additional considerations. Always refer to the documentation of the specific tools or platforms you're using for detailed instructions. 

## Sources
Source code of ChatTheDoc is available in Github https://github.com/teddyyayo/AI2PdfChat