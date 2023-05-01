import os
import pygame
import time
import io
import requests
import sys
from PIL import Image

def api_get_state(ip_address):  
    url = f"http://{ip_address}:5016/state/started"
    response = requests.get(url)
    if response.status_code == 200:
        return response.content == b'true\n'
    else:
        print("chyba")


#state = true to start
def api_state_change(ip_address, state):
    if(state):
        newState="start"
    else:    
        newState="stop"    
    url = f"http://{ip_address}:5016/state/{newState}"
    print(url)
    response = requests.get(url)
    if response.status_code == 200:
        print("ok")
    else:
        print("chyba")

def api_color_img(ip_address):
    state=api_get_state(ip_address)
    if(not state):
        api_state_change(ip_address,True)


    url = f"http://{ip_address}:5016/color/image"
    response = requests.get(url)
    if(not state):
        api_state_change(ip_address,False)
    if response.status_code == 200:
        image = Image.open(io.BytesIO(response.content))
        return image
    else:
        return None


if len(sys.argv) == 2:
    ip_address = sys.argv[1]
else:
    ip_address="127.0.0.1"


pygame.init()
# Set display mode to fullscreen
screen = pygame.display.set_mode((0, 0), pygame.FULLSCREEN)
# Set display time per image in seconds
display_time = 0.6

# Set up the image capture directory
output_dir_path = "capture"
if not os.path.exists(output_dir_path):
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

    result = api_color_img(ip_address)
    if result is None:
        print("Error calling API.")
        exit()
        
    formatted_num = '{:02d}'.format(i)
    output_file_path = os.path.join(output_dir_path, f'graycode_{formatted_num}.jpg')
    result.save(output_file_path)
    i+=1       
    
# Quit Pygame
pygame.quit()
