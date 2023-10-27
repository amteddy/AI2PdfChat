import os
import PyPDF2 as pydef2 #to read pdfs

# LangChain as a language model integration to link large language models (LLMs) with 
# Python and work with documents like PDFs & databases.
from langchain.chat_models import ChatOpenAI
from langchain.embeddings.openai import OpenAIEmbeddings
from langchain.document_loaders import PyPDFLoader
from langchain.text_splitter import RecursiveCharacterTextSplitter
from langchain.chains import RetrievalQA
from langchain.chains import ConversationalRetrievalChain 

# FAISS (Facebook AI Similarity Search) library to quickly search for embeddings 
# in documents that are similar to each other '''
from langchain.vectorstores import FAISS

#ToDo: Get key from OpenAI and add it to OPENAI_API_KEY ENV variable
os.environ["OPENAI_API_KEY"]
text_detected_from_pdf = ""
chat_history = []
vector_index = ""
initialized = False
target_path= os.path.join(os. getcwd(), "data", "text_detected_from_pdf.txt")

#***********************************************************************************
# Read one pdf file using PyPDF2
# ToDO: Read multiple files
#***********************************************************************************
def import_pdf(pdf_path):    
    global text_detected_from_pdf
    print("...importing pdf: " + pdf_path)
    imported_pdf_file = open(pdf_path, "rb")
    pdf_reader = pydef2.PdfFileReader(imported_pdf_file)

    for page_number in range(pdf_reader.numPages):
        page = pdf_reader.getPage(page_number)
        text_detected_from_pdf += page.extractText() + "\n\n"

    imported_pdf_file.close()
    print("*** Imported file: \n " + pdf_path)
    f = open(target_path, "w", encoding='utf-8')
    f.write(text_detected_from_pdf)
    f.close()

#***********************************************************************************
# read multiple pdf files in directory
#***********************************************************************************
def import_all_pdfs_in_directory(dir):
    file_count = 0
    if os.path.isdir(dir):        
        for f in os.listdir(dir):
            file = os.path.join(dir, f)
            if os.path.isfile(file) and file.endswith(".pdf"):
                import_pdf(file)
                file_count += 1
        print("=== Imported files: " + str(file_count))
    else:
        return "No files or directory found!"

#***********************************************************************************
# Perform chunking and split the text using LangChain text splitters.
#***********************************************************************************
def prepare_imported_data_for_chat():
    global text_detected_from_pdf, vector_index
    text_chunk_splitter = RecursiveCharacterTextSplitter(chunk_size=1200, chunk_overlap=200)
    if(text_detected_from_pdf == ""):
        file = open(target_path, mode='r')
        text_detected_from_pdf = file.read()
    texts = text_chunk_splitter.create_documents([text_detected_from_pdf])

    # Use FAISS vector store and save vectors to a file.
    vector_index = FAISS.from_documents(texts, OpenAIEmbeddings())
    vector_index.save_local("vector_store")
    vector_index = FAISS.load_local("vector_store", OpenAIEmbeddings())
    return vector_index

#***********************************************************************************
# Initialize chat
# Load db and configure a retriever to create a chat object. 
# This chat object (qa_interface) is used to have the initial/first chat with the PDF .
#***********************************************************************************
def init_chat(question, retriever):
    global initialized
    retrieval_qa_interface = RetrievalQA.from_chain_type(
        llm=ChatOpenAI(),
        chain_type="stuff",
        retriever=retriever,
        return_source_documents=True,
    )

    # Chat with the PDF using RetrievalQA from LangChain to pull document pieces from a vector store and ask. 
    response = retrieval_qa_interface(question)
    initialized = True
    return response 


#***********************************************************************************
# Continue Chat
# Using ConversationalRetrievalChain for conversation history 
#***********************************************************************************
def continue_chat(question):
    global chat_history, initialized, vector_index, count
    print(question)
    retriever = vector_index.as_retriever(search_type="similarity", search_kwargs={"k": 6})        
    if(not initialized)  :
        response = init_chat(question, retriever)
        print(response["result"])
        chat_history.append((question, response["result"]))
        return response["result"]
    else:        
        retrieval_conv_interface = ConversationalRetrievalChain.from_llm(ChatOpenAI(temperature=0), retriever=retriever)
        result = retrieval_conv_interface({"question": question, "chat_history": chat_history})
            
        chat_history.append((question, result["answer"]))
        print(result["answer"])
    return result["answer"]

#***********************************************************************************
# get chat history
#***********************************************************************************
def get_chat_history():	
    return chat_history

#***********************************************************************************
# enter your question to chat
#***********************************************************************************
def ask_to_continue():	
    user_input = input("Enter your Question to AI agent...\n")
    result = continue_chat(user_input)
    ask_to_continue() #as user's choice to continue playing the game

def initialize_vector():
    global vector_index
    vector_index = prepare_imported_data_for_chat()

''' Example use cases below
Note: import_all_pdfs_in_directory or import_pdfs need to be called
atleast once  and initialize_vector functions need to follow. 
ask_to_continue funtion is needed only when you chat from this python directly.
'''
#import_pdfs("pdfs/test.pdf")
#import_all_pdfs_in_directory("pdfs") #not needed if it is once done, unless pdfs are updated
#initialize_vector()
#ask_to_continue()

