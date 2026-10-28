from PIL import Image
import sys

def remove_white(input_path, output_png, output_ico):
    img = Image.open(input_path).convert("RGBA")
    data = img.getdata()
    
    new_data = []
    for item in data:
        # Change white (also shades of white) to transparent
        if item[0] > 240 and item[1] > 240 and item[2] > 240:
            new_data.append((255, 255, 255, 0))
        else:
            new_data.append(item)
            
    img.putdata(new_data)
    
    # Save as PNG
    img.save(output_png, "PNG")
    
    # Save as ICO (multiple sizes)
    img.save(output_ico, format="ICO", sizes=[(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    print("Done")

remove_white(sys.argv[1], sys.argv[2], sys.argv[3])
