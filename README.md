# ImageNet-Viewer
Simple web application showing structure of an ImageNet XML file.This tool allows users to visualize and explore the hierarchical structure of ImageNet XML files, facilitating better understanding and analysis of the dataset.


## Usage
1. Clone the repository or download the [zip file](https://github.com/richard-ciglansky/imagenet-viewer/archive/refs/heads/master.zip).
2. Navigate to the project directory in your terminal.
3. Run `docker compose up -d` to start the application.
4. Access the application at http://localhost:6789 in your web browser.


## Features

Application allows navigation through the hierarchy of classes and images.
- Users can explore the class hierarchy.
- Supports filtering and searching for specific search term.
- Allows users to navigate through the hierarchy using intuitive controls.
- Allows users to download subtree for a node in JSON format.
- Data are loaded lazily on node expansion.
- When search is engaged by entering a searchterm, only matched tree branches are shown down to the point of match.
  Search term is highlighted in the tree.
  From this point downwards all tree branches are shown.
- Database uses non-sa account to access data


## Internals

- Input parsing of XML is done offline.
- It loads XML into memory in whole because the data file is relatively small.   
    When the size of the input would not be known in advance, XmlReader should be used instead to prevent memory exhaustion.
    As size of a node would be known just after all children are loaded, the nodes will be outputed in reverse order.   
    This should be reflected in the code by not using natural ordering defined by CLUSTERED index on Name field.
- Counts total descendants of each node In O(N) time.   
    This is implemented by one recursive traversal of the tree.
- Export the tree into JSON array by formatting elements explicitly to prevent creation of big structures in memory.
- Extra fields are added to nodes to store sequential Id to preserve ordering of nodes, tree level to help in navigation and parent node Id for future navigation needs.
- Outputs JSON array to client in O(N) time.
- Outputs maximum length of node 'word' field and maximum length of the the path in the tree to help decide maximum size of database columns.

- Webservice on start reinitialze database and load JSON array from file into database.
- Provides enpoints for loading children in array format, loading subtree in tree-base JSON format and searching for a term in nodes.
- Application is accessible on non-standard port 6789 to prevent colision with other applications.
- Internally run SQL Server in docker container is exposed on port 9876 to prevent colision with MS SQL Server on the host machine.


## Known Isuess
- When searching is engaged, the numbers of descendants of the matched node are not shown, just number of immedate children.   
    This can be fixed by adding new endpointsm or less effectively by loading nodes one-by-one. 
- Application uses of preloaded data to optimize performance. It detects the maximum size of database fields to create database table accordingly.  
    This can be improved by using Import directly in the web service during initialization, optimalization of the table structure will be more complicated.
    And as performance was main focus of this application, it was not implemented intentionally.
- Passwords for database are exposed in docker setup.  
    As this is a demo application, it is not a problem.
- Application is not ready for handling big data, as for this pagination for all endpoints is required.  
    It can be implemented easily by adding new parameters 'pageToken' and 'pageSize' to all endpoints and updating database query to return just nodes
    with Name > pageToken and at most pageSize nodes. Pagination, when used right, will however complicate the client side code much more.
- Search is not case insensitive.  
    This is not a big problem, as most of the data is in lower case anyway.