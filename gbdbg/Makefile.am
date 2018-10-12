EXTRA_DIST =

COMPILE_TARGET = console
ASSEMBLY_COMPILER = CSC
CSC_ARGS = $(CSC_NO_UNSAFE) $(CSC_STRONGNAME)

all: $(ASSEMBLY) $(LINUX_PKGCONFIG) $(ASSEMBLY_WRAPPER)

FILES = \
	InvalidResponseException.cs \
	Lr35902Debugger.cs \
	Lr35902Debugger.TargetMemoryStream.cs \
	Lr35902Disassembler.cs \
	Lr35902Registers.cs \
	Lr35902StatusFlags.cs \
	NotHaltedException.cs \
	Program.cs \
	Properties/AssemblyInfo.cs

DATA_FILES =

RESOURCES =

EXTRAS =

REFERENCES = System

PROJECT_REFERENCES =

DLL_REFERENCES =

CLEANFILES =

include $(top_srcdir)/Makefile.include
