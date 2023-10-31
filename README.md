# AI2PdfChat
AI2PdfChat implments AI logic to make users interact to their pdf files and get information without opening and reading them - just by asking in a chat. 
The implmentation has a blazor based UI with python implmentation of the AI logic to interact with OpenAi's Chat GPT. 

## Required libraries to install 
* PyPDF2 library for extracting PDFs with digital text
 ```pip install PyPDF2 ```

* LangChain
LangChain is used here for linking large language models (LLMs) with Python and large PDFs
 ```pip install langchain openai pypdf faiss-cpu ```

* to chat using open-ai
 ```pip install --upgrade openai ```

* FAISS (Facebook AI Similarity Search)
FAISS is used to search for embeddings of multimedia documents that are similar to each other.
More info in https://ai.meta.com/tools/faiss/

* nuget package Python.net to run Python from C#
https://www.nuget.org/packages/pythonnet

* .NET Install 
Download and install .Net 6.0. Install it from .NET Framework Developer Packs at https://aka.ms/msbuild/developerpacks

* install Microsoft Visual C++
Microsoft Visual C++ 14.0 or greater is required. Get it with "Microsoft C++ Build Tools": https://visualstudio.microsoft.com/visual-cpp-build-tools/

## How to use AI2PdfChat
- Create a .env file with the following name
     ```OPENAI_API_KEY=Your_OpenAI_API_Key ```
- Set envronment variable to your python dll e.g.
     ```PYTHONNET_PYDLL  = "C:\Python\python39.dll" ```
- Copy your pdf documents to data/sample_pdfs directory.
- Run the tool (AI2PdfChat)
- Start chatting to the pdf by inserting your questions. AI2PdfChat will search through the pdf and gives you the desired solution

## Sources
* All source codes are available in Github https://github.com/amteddy/AI2PdfChat
