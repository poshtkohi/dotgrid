# DotGrid: A .NET-based middleware for Grid and Cloud Computing

During my BSc studies, I developed a cross-platform grid software. It is a low-level middleware system grounded on a specialized concept of distributed objects and  ECMA.NET-compliant execution for highly concurrent distributed systems, to make writing middleware easier on heterogeneous platforms. It takes care of low-level network programming interfaces for Grid/Cloud-specific platforms and allows the middleware architects to focus their efforts on their middleware logic. DotThread provides the capability of remote code execution, dynamic distributed object registration and activation, transparent communication on the underlying transport protocols, data marshaling and unmarshaling, distributed operation dispatching, checkpoint/restore, etc. DotSec introduces techniques for the authentication of users and secure communication. DotDFS proposes utilities and libraries for transmitting, storing and managing massive data sets. The platform was purely developed in C# and .NET Framework. The source code of this project can be found in the source directory of this repository.

**Licence:** Note that the code can be changed and reused as long as you keep the copyright inside and at the beginning of every C# file in the source directory unchanged. If this framework is used in a research project that ends up with a publication, it should be cited as below

**[1]** A. Poshtkohi, M. B. Ghaznavi-Ghoushchi, _DotDFS: A Grid-based High-Throughput File Transfer System_, Parallel Computing, 37 (2011) 114–136., doi: 10.1016/j.parco.2010.12.003

**[2]** A. Poshtkohi, A.H. Abutalebi, S. Hessabi, _DotGrid: A .NET-based Cross-Platform Software for Desktop Grids_, Int. J. Web Grid Serv. 3 (3) (2007) 313–332., doi: 10.1504/IJWGS.2007.014955

The architecture of the DotGrid framework is shown below

![DotGrid architecture](/assets/images/architecture.png)
