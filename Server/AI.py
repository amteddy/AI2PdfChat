from importlib.abc import FileLoader
import os
import sys
from tracemalloc import start

from langchain.chat_models import ChatOpenAI 
from langchain.embeddings.openai import OpenAIEmbeddings
from langchain.document_loaders import PyPDFLoader
from langchain.chains import ConversationalRetrievalChain 
from langchain.memory import ConversationBufferMemory
from langchain.callbacks.streaming_stdout import StreamingStdOutCallbackHandler
from langchain.text_splitter import RecursiveCharacterTextSplitter
from langchain.vectorstores import Chroma
from datetime import datetime
from langchain.document_loaders.merge import MergedDataLoader

class StreamingStdOutCallbackHandler(StreamingStdOutCallbackHandler):
    def __init__(self, callback):
        self.callback = callback
        self.original_stdout = sys.stdout
        self.data = None
        self.active = False 
        self.user_to_respond_to = ""       

    def write(self, data):
        if self.active:            
            self.callback({"user": self.user_to_respond_to, "data": data})            

    def activate_callback(self, active):
        self.active = active

class AiClass:
    open_ai_key = os.environ["OPENAI_API_KEY"]
    chat_history = []
    vector_index = {}
    data_dir = os.path.join(os.path.dirname(os.path.realpath(__file__)), "data")    
    sample_pdfs_dir = os.path.join(data_dir, "sample_pdfs")

    user_callbacks = {}

    if(not os.path.exists(data_dir)):
        os.makedirs(data_dir)

    if(not os.path.exists(sample_pdfs_dir)):
        os.makedirs(sample_pdfs_dir)

    def __init__(self):
        self.status_update_event = None
        self.user = None
    
    def set_status_update_event(self, user, event_handler):        
        self.status_update_event = event_handler 
        self.user_callbacks[user] = StreamingStdOutCallbackHandler(callback = event_handler)
        self.user_callbacks[user].user_to_respond_to = user

    #***********************************************************************************
    # Read pdf
    #***********************************************************************************
    def import_pdf(self, pdf_path):            
        pdf_reader = PyPDFLoader(pdf_path)
        print("Imported document: " + os.path.basename(pdf_path))
        return pdf_reader
    
    #***********************************************************************************
    # Read multiple pdf files in directory
    #***********************************************************************************
    def import_all_pdfs_in_directory(self, dir):
        file_count = 0
        pdf_pages = []
        topic = os.path.basename(dir)  
        if os.path.isdir(dir):
            if not os.listdir(dir): 
                print("No documents found in: " + dir)
            else:         
                for f in os.listdir(dir):
                    file = os.path.join(dir, f)                    
                    if os.path.isfile(file) and file.endswith(".pdf"):
                        pdf_pages.append(self.import_pdf(file))
                        file_count += 1
            print("Imported files: " + str(file_count))

            if(file_count > 0): 
                ''' Perform chunking and split the text using LangChain recursive text splitters. '''       
                print("Preparing imported data and creating vectore store...")            
                text_splitter = RecursiveCharacterTextSplitter(chunk_size=1000, chunk_overlap=100)
                combined_pdfs = MergedDataLoader(loaders=pdf_pages)
                
                documents = text_splitter.split_documents(combined_pdfs.load())
                self.vector_index[topic] = Chroma.from_documents(documents, OpenAIEmbeddings(), persist_directory=os.path.join(self.data_dir, "vector_store", topic))
                print("Chunking and spliting done for topic: ", topic) 
                print("_______________________________________________")
            
            else:
                print("No files to be imported!")
        else:
            return "No files or directory found!"  

    #***********************************************************************************
    # Loads vector from file
    #***********************************************************************************
    def load_vector_store_from_existing(self, topic):
        self.vector_index[topic] = Chroma(persist_directory=os.path.join(self.data_dir, "vector_store", topic), embedding_function=OpenAIEmbeddings())    
        print("Loading vector from file done.")
    
    #***********************************************************************************
    # Chat using ConversationalRetrievalChain 
    #******************what *****************************************************************
    def chat(self, user, question, topic):
        start_time = datetime.now()
        print(start_time.strftime("%H:%M:%S.%f"), "   Question asked: ",  question)    
        
        if(not self.vector_index.get(topic)):
            self.load_vector_store_from_existing(topic)
        if(self.vector_index.get(topic)):                             
            llm =  ChatOpenAI(streaming=True, callbacks=[self.user_callbacks[user]], temperature=0.7)
            memory = ConversationBufferMemory(memory_key="chat_history", return_messages=True) 
            retrieval_conv_interface = ConversationalRetrievalChain.from_llm(llm, retriever=self.vector_index[topic].as_retriever(lambda_val=0.025, k=5, filter=None) , memory=memory)                      
            original_std_out = sys.stdout            
            sys.stdout = self.user_callbacks[user]
            self.user_callbacks[user].activate_callback(active=True)
            result = retrieval_conv_interface({"question": question})   
            self.user_callbacks[user].activate_callback(active=False)
            sys.stdout =  original_std_out

            self.chat_history.append((question, result["answer"]))  

            end_time = datetime.now()   
            print(end_time.strftime("%H:%M:%S.%f"), "   Question Answered") 
            diff = end_time - start_time
            print("Answered in TIME:  ", diff)          
            return result["answer"]
    
    #***********************************************************************************
    # Ask to chat
    #***********************************************************************************
    def ask_to_continue(self, topic):
        print("\n_______________________________________________")
        user_input = input("Enter your Question to AI agent...\n")
        #ai.user_callbacks["Demo User"] = StreamingStdOutCallbackHandler
        self.chat("Demo User", user_input, topic)
        self.ask_to_continue()

    ''' Example use cases below
    Note: import_all_pdfs_in_directory or import_pdfs need to be called atleast once  and initialize functions need to follow. 
    ask_to_continue funtion is needed only when you chat from this python directly.
    '''

'''
ai = AiClass()
dir = os.path.join(os.path.dirname(os.path.realpath(__file__)), "data")
#ai.import_all_pdfs_in_directory(os.path.join(dir, "sample_pdfs", "SCIL")) #not needed if it is once done, unless pdfs are updated
#ai.load_vector_store_from_existing("SCIL")    
ai.ask_to_continue("SCIL")
'''