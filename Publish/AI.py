from importlib.abc import FileLoader
import os
import time
from tracemalloc import start
# LangChain language model integration to link large language models (LLMs) 
from langchain.chat_models import ChatOpenAI 
from langchain.embeddings.openai import OpenAIEmbeddings
from langchain.document_loaders import PyPDFLoader
from langchain.chains import ConversationalRetrievalChain 
from langchain.memory import ConversationBufferMemory
from langchain.callbacks.streaming_stdout import StreamingStdOutCallbackHandler
from langchain.vectorstores import FAISS # Facebook AI Similarity Search to search for embeddings in documents that are similar to each other
from langchain.embeddings.openai import OpenAIEmbeddings
from langchain.text_splitter import CharacterTextSplitter
from langchain.vectorstores import Chroma
from datetime import date, datetime
from langchain.memory import ConversationBufferMemory

class StatusUpdateEventArgs(StreamingStdOutCallbackHandler):
    def __init__(self):
        self.data = ""
    def on_data(self, data):
        super().on_data(data)
        self.data = data
        self.data_event_set()

class AiClass:
    open_ai_key = os.environ["OPENAI_API_KEY"]
    pdf_pages = []
    chat_history = []
    vector_index = []
    data_dir = os.path.join(os.path.dirname(os.path.realpath(__file__)), "data")
    sample_pdfs_dir = os.path.join(data_dir, "sample_pdfs")
    
    callback = StatusUpdateEventArgs()

    if(not os.path.exists(data_dir)):
        os.makedirs(data_dir)

    if(not os.path.exists(sample_pdfs_dir)):
        os.makedirs(sample_pdfs_dir)

    def __init__(self):
        self.status_update_event = None
    
    def set_status_update_event(self, event_handler):
        self.status_update_event = event_handler    

    #***********************************************************************************
    # Read pdf
    #***********************************************************************************
    def import_pdf(self, pdf_path):    
        print("Importing document: " + os.path.basename(pdf_path))
        pdf_reader = PyPDFLoader(pdf_path)
        self.pdf_pages = pdf_reader.load_and_split()
    
    #***********************************************************************************
    # Read multiple pdf files in directory
    #***********************************************************************************
    def import_all_pdfs_in_directory(self, dir):
        file_count = 0
        if os.path.isdir(dir):        
            for f in os.listdir(dir):
                file = os.path.join(dir, f)
                if os.path.isfile(file) and file.endswith(".pdf"):
                    self.import_pdf(file)
                    file_count += 1
            print("_______________________________________________")
            print("Imported files: " + str(file_count))
        else:
            return "No files or directory found!"

    #***********************************************************************************
    # Perform chunking and split the text using LangChain text splitters.
    #***********************************************************************************
    def prepare_imported_data_for_chat(self):
        if not os.listdir(self.sample_pdfs_dir): 
            print("No documents found in: " + self.sample_pdfs_dir) 
        else:
            print("Preparing imported data and creating vectore store...")            
            text_splitter = CharacterTextSplitter(chunk_size=1000, chunk_overlap=100)
            documents = text_splitter.split_documents(self.pdf_pages)
            self.vector_index = Chroma.from_documents(documents, OpenAIEmbeddings(), persist_directory=os.path.join(self.data_dir, "vector_store"))
            print("Preparation done.")
        return self.vector_index

    #***********************************************************************************
    # Loads vector from file
    #***********************************************************************************
    def load_vector_store_from_existing(self):
        self.vector_index = Chroma(persist_directory=os.path.join(self.data_dir, "vector_store"), embedding_function=OpenAIEmbeddings())    
        print("Loading vector from file done.")
    
    #***********************************************************************************
    # Chat using ConversationalRetrievalChain 
    #******************what *****************************************************************
    def chat(self, question):
        start_time = datetime.now()
        print(start_time.strftime("%H:%M:%S.%f"), "   Question asked: ",  question)    

        if(self.vector_index == []):
            self.load_vector_store_from_existing()
        if(self.vector_index != []):                       
            llm =  ChatOpenAI(streaming=True, callbacks=[self.callback], temperature=0.6)
            memory = ConversationBufferMemory(memory_key="chat_history", return_messages=True) 
            retrieval_conv_interface = ConversationalRetrievalChain.from_llm(llm, retriever=self.vector_index.as_retriever(lambda_val=0.025, k=5, filter=None) , memory=memory)                      
            result = retrieval_conv_interface({"question": question})   
            if self.status_update_event:
                print("Updating..." + self.callback.data)
                self.status_update_event(self.callback.data)                       
            self.chat_history.append((question, result["answer"]))  

            end_time = datetime.now()   
            print(end_time.strftime("%H:%M:%S.%f"), "   Question Answered") 
            diff = end_time - start_time
            print("Answered in TIME:  ", diff)          
            return result["answer"]

    #***********************************************************************************
    # get chat history
    #***********************************************************************************
    def get_chat_history(self):	
        return self.chat_history

    #***********************************************************************************
    # Ask to chat
    #***********************************************************************************
    def ask_to_continue(self):
        print("\n_______________________________________________")
        user_input = input("Enter your Question to AI agent...\n")
        result = self.chat(user_input)
        self.ask_to_continue() #as user's choice to continue playing the game

    #***********************************************************************************
    # Init Vector
    #***********************************************************************************
    def initialize(self):    
        self.vector_index = self.prepare_imported_data_for_chat()

    ''' Example use cases below
    Note: import_all_pdfs_in_directory or import_pdfs need to be called atleast once  and initialize functions need to follow. 
    ask_to_continue funtion is needed only when you chat from this python directly.
    '''
    #import_pdfs("sample_pdfs/test.pdf")
    #import_all_pdfs_in_directory(os.path.join(data_dir, "sample_pdfs")) #not needed if it is once done, unless pdfs are updated
    #initialize()

    #load_vector_store_from_existing()
    #ask_to_continue()
