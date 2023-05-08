import os
import pygame
import time
import io
import requests
import sys
from PIL import Image

def api_get_state(ip_address,port):
    url = f"http://{ip_address}:{port}/state/started"
    response = requests.get(url)
    if response.status_code == 200:
        return response.content == b'true\n'
    else:
        print("chyba")
        exit()


#state = true to start
def api_state_change(ip_address, port, state):
    if(state):
        newState="start"
    else:    
        newState="stop"    
    url = f"http://{ip_address}:{port}/state/{newState}"
    print(url)
    response = requests.get(url)
    if response.status_code == 200:
        print("ok")
    else:
        print("chyba")
        exit()

def api_color_img(ip_address, port):
    state=api_get_state(ip_address, port)
    #if(not state):
    #    api_state_change(ip_address, port,True)
    url = f"http://{ip_address}:{port}/color/image"
    response = requests.get(url)
    #if(not state):
     #   api_state_change(ip_address, port, False)
    if response.status_code == 200:
        image = Image.open(io.BytesIO(response.content))
        return image
    else:
        return None


if len(sys.argv) == 2:
    ip_address = sys.argv[1]
elif len(sys.argv) == 3:
    ip_address = sys.argv[1]
    port = sys.argv[2]
else:
    ip_address = "127.0.0.1"
    port = "5016"


pygame.init()
screen = pygame.display.set_mode((0, 0), pygame.FULLSCREEN)
# Set display time per image in seconds
display_time = 0.6

output_dir_path = "capture_"
j = 1

while os.path.exists(f"{output_dir_path}{j}"):
    j += 1
output_dir_path+=str(j)
os.makedirs(output_dir_path)

folder = "graycode_pattern"

i=0
for filename in os.listdir(folder):
    input_file_path = os.path.join(folder, filename)
    image = pygame.image.load(input_file_path)
    image_surface = pygame.Surface.convert_alpha(image)
    image_surface = pygame.transform.scale(image_surface, screen.get_size())
    screen.blit(image_surface, (0, 0))
    pygame.display.flip()
    time.sleep(display_time)

    result = api_color_img(ip_address, port)
    if result is None:
        print("Error calling API.")
        exit()
        
    formatted_num = '{:02d}'.format(i)
    output_file_path = os.path.join(output_dir_path, f'graycode_{formatted_num}.jpg')
    result.save(output_file_path)
    i+=1       
    
pygame.quit()
