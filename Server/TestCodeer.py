import time

class StatusUpdateEventArgs:
    def __init__(self, status):
        self.status = status

class MainPythonClass:
    def __init__(self):
        self.status_update_event = None
    
    def set_status_update_event(self, event_handler):
        self.status_update_event = event_handler

    def execute(self):
        for i in range(100):
            status = f"Processing step {i + 1}"

            if self.status_update_event:
                self.status_update_event(status)
            time.sleep(1)
        
        return "Exec done!"