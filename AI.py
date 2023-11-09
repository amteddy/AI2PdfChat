import os
# LangChain language model integration to link large language models (LLMs) 
from langchain.chat_models import ChatOpenAI 
from langchain.embeddings.openai import OpenAIEmbeddings
from langchain.document_loaders import PyPDFLoader
from langchain.chains import ConversationalRetrievalChain 
from langchain.memory import ConversationBufferMemory
from langchain.callbacks.streaming_stdout import StreamingStdOutCallbackHandler
from langchain.vectorstores import FAISS # Facebook AI Similarity Search to search for embeddings in documents that are similar to each other

open_ai_key = os.environ["OPENAI_API_KEY"]
pdf_pages = []
chat_history = []
vector_index = []
data_dir = os.path.join(os.path.dirname(os.path.realpath(__file__)), "data")
sample_pdfs_dir = os.path.join(data_dir, "sample_pdfs")

if(not os.path.exists(data_dir)):
    os.makedirs(data_dir)

if(not os.path.exists(sample_pdfs_dir)):
    os.makedirs(sample_pdfs_dir)

#***********************************************************************************
# Read pdf
#***********************************************************************************
def import_pdf(pdf_path):    
    global pdf_pages
    print("Importing document: " + os.path.basename(pdf_path))
    pdf_reader = PyPDFLoader(pdf_path)
    pdf_pages = pdf_reader.load_and_split()
   
#***********************************************************************************
# Read multiple pdf files in directory
#***********************************************************************************
def import_all_pdfs_in_directory(dir):
    file_count = 0
    if os.path.isdir(dir):        
        for f in os.listdir(dir):
            file = os.path.join(dir, f)
            if os.path.isfile(file) and file.endswith(".pdf"):
                import_pdf(file)
                file_count += 1
        print("_______________________________________________")
        print("Imported files: " + str(file_count))
    else:
        return "No files or directory found!"

#***********************************************************************************
# Perform chunking and split the text using LangChain text splitters.
#***********************************************************************************
def prepare_imported_data_for_chat():
    global pdf_pages, vector_index
    if not os.listdir(sample_pdfs_dir): 
        print("No documents found in: " + sample_pdfs_dir) 
    else:
        print("Preparing imported data and creating vectore store...")        
        # Use FAISS vector store and save it to a file.        
        vector_index = FAISS.from_documents(pdf_pages, OpenAIEmbeddings())
        vector_index.save_local(os.path.join(data_dir, "vector_store"))
        vector_index = FAISS.load_local(os.path.join(data_dir, "vector_store"), OpenAIEmbeddings())
        print("Preparation done.")
    return vector_index

#***********************************************************************************
# Chat using ConversationalRetrievalChain 
#***********************************************************************************
def chat(question):
    global chat_history, vector_index
    print(question)
    if(vector_index != ""):
        retriever = vector_index.as_retriever(search_type="similarity", search_kwargs={"k": 1})                
        retrieval_conv_interface = ConversationalRetrievalChain.from_llm(
            ChatOpenAI(streaming=True, 
            callbacks=[StreamingStdOutCallbackHandler()], 
            temperature=1),
            retriever=retriever)      
        result = retrieval_conv_interface({"question": question, "chat_history": chat_history})                         
        chat_history.append((question, result["answer"]))        
        return result["answer"]

#***********************************************************************************
# get chat history
#***********************************************************************************
def get_chat_history():	
    return chat_history

#***********************************************************************************
# Ask to chat
#***********************************************************************************
def ask_to_continue():
    print("\n_______________________________________________")
    user_input = input("Enter your Question to AI agent...\n")
    result = chat(user_input)
    ask_to_continue() #as user's choice to continue playing the game

#***********************************************************************************
# Init Vector
#***********************************************************************************
def initialize():
    global vector_index
    vector_index = prepare_imported_data_for_chat()

''' Example use cases below
Note: import_all_pdfs_in_directory or import_pdfs need to be called atleast once  and initialize functions need to follow. 
ask_to_continue funtion is needed only when you chat from this python directly.
'''
#import_pdfs("sample_pdfs/test.pdf")
#import_all_pdfs_in_directory(os.path.join(data_dir, "sample_pdfs")) #not needed if it is once done, unless pdfs are updated
#initialize()
#ask_to_continue()
