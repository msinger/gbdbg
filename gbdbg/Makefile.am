EXTRA_DIST =

COMPILE_TARGET = console
ASSEMBLY_COMPILER = CSC
CSC_ARGS = $(CSC_NO_UNSAFE) $(CSC_STRONGNAME)

all: $(ASSEMBLY) $(LINUX_PKGCONFIG) $(ASSEMBLY_WRAPPER)

FILES = \
	AsmFormatException.cs \
	InvalidResponseException.cs \
	Lr35902Assembler.cs \
	Lr35902Assembler.Addition.cs \
	Lr35902Assembler.Argument.cs \
	Lr35902Assembler.DataDirective.cs \
	Lr35902Assembler.Indirection.cs \
	Lr35902Assembler.Instruction.cs \
	Lr35902Assembler.Label.cs \
	Lr35902Assembler.Negation.cs \
	Lr35902Assembler.OriginDirective.cs \
	Lr35902Assembler.ParserToken.cs \
	Lr35902Assembler.PostDecrement.cs \
	Lr35902Assembler.PostIncrement.cs \
	Lr35902Assembler.Substraction.cs \
	Lr35902Assembler.Terminal.cs \
	Lr35902Debugger.cs \
	Lr35902Debugger.TargetMemoryStream.cs \
	Lr35902Disassembler.cs \
	Lr35902LexerBase.cs \
	Lr35902LexerBase.LexerToken.cs \
	Lr35902LexerBase.LexerTokenType.cs \
	Lr35902Registers.cs \
	Lr35902StatusFlags.cs \
	NameParser.cs \
	NotHaltedException.cs \
	NumberParser.cs \
	Program.cs \
	Properties/AssemblyInfo.cs \
	Range.cs

DATA_FILES =

RESOURCES =

EXTRAS =

REFERENCES = System

PROJECT_REFERENCES =

DLL_REFERENCES =

CLEANFILES =

include $(top_srcdir)/Makefile.include
