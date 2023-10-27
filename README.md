# AI2PdfChat
This contains source codes of the AI logic to make users interact to their pdf files and get information without opening and reading them - just by asking in a chat. The implmentation has a blazor based UI with python implmentation of the AI logic to interact with Open AI's Chat GPT. 

# PyPDF2 library for extracting PDFs with digital text
pip install PyPDF2

# to chat using open-ai
pip install --upgrade openai

# Create a .env file with the following name
OPENAI_API_KEY=Your_OpenAI_API_Key

# for openAi embeddings
pip install tiktoken

# LangChain is used here for linking large language models (LLMs) with Python and large PDFs
pip install langchain openai pypdf faiss-cpu

# Create vector of the pdfs.
Copy pdf to a folder
Read them using pdf functoi
Ingest them using npm

# nuget package Python.net to run Python from C#
https://www.nuget.org/packages/pythonnet
Set envronment variable to your python dll e.g.
PYTHONNET_PYDLL  = "C:\Python\python39.dll"


All source codes are available in Github https://github.com/amteddy/AI2PdfChat