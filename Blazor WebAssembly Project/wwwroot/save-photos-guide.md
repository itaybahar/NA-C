# Photo Installation Guide

To complete your home page setup, you need to save the 8 photos you shared to the `images` folder with these exact names:

## Required Photo Names:
1. `group-photo-1.jpg` - First group photo with military uniforms and banners
2. `group-photo-2.jpg` - Gymnasium group photo with red fabric overhead
3. `beach-photo.jpg` - Beach sunset photo with red banner
4. `sports-team-1.jpg` - Sports team photo with red banners and uniforms
5. `sports-team-2.jpg` - Group of young women with red banners on court
6. `stadium-crowd-1.jpg` - Stadium crowd photo with people in red
7. `stadium-crowd-2.jpg` - Another stadium photo with fans in red and black
8. `stadium-fans.jpg` - Stadium bleacher photo with group in red and black

## How to Save:
1. Right-click on each photo in your conversation
2. Select "Save Image As..."
3. Save to: `Blazor WebAssembly Project/wwwroot/images/`
4. Use the exact file names listed above
5. Save as JPG format

## Alternative Method:
If you have all photos in a folder, you can rename them according to the list above and copy them to the images directory.

## File Locations:
All photos should be saved to:
```
Blazor WebAssembly Project/
└── wwwroot/
    └── images/
        ├── group-photo-1.jpg
        ├── group-photo-2.jpg
        ├── beach-photo.jpg
        ├── sports-team-1.jpg
        ├── sports-team-2.jpg
        ├── stadium-crowd-1.jpg
        ├── stadium-crowd-2.jpg
        └── stadium-fans.jpg
```

Once you save all 8 photos with the correct names, your home page carousel will automatically display them with smooth transitions! 

## **How to Connect Blazor WASM and API on the Same Port**

### **1. Publish Blazor to the API's wwwroot**

From your solution root, run this command in the terminal:

```sh
dotnet publish "Blazor WebAssembly Project/Blazor WebAssembly Project.csproj" -c Release -o "../API Project/wwwroot"
```
- This will build your Blazor app and copy all its static files into the API project's `wwwroot` folder.

### **2. Run Only the API Project**

- Stop the Blazor dev server (if running).
- Start only the API project (in Visual Studio: set API Project as startup and run, or use `dotnet run` in the API Project folder).

Now, when you browse to `https://localhost:7235` (or whatever port your API uses), you will get the Blazor app, and all API endpoints will work on the same port.

### **3. (Optional) Automate the Publish Step**

You can add a post-build step or script to always copy the Blazor output to the API's `wwwroot` after building.

### **4. Update Your Redirect URIs (if needed)**

In Google Cloud Console, make sure your redirect URI matches the API port, e.g.:
```
https://localhost:7235/auth/google-callback
```

### **5. Test**

- Open `https://localhost:7235` in your browser.
- All Blazor navigation, login, and API calls should work on the same port.

## **Summary**

- **Publish Blazor WASM to API's wwwroot**
- **Run only the API project**
- **Browse to the API port (e.g., https://localhost:7235)**

## **Summary**

- **Publish Blazor WASM to API's wwwroot**
- **Run only the API project**
- **Browse to the API port (e.g., https://localhost:7235)**

If you want, I can provide a script or MSBuild step to automate the publish/copy process.  
Let me know if you need that or if you have any issues! 