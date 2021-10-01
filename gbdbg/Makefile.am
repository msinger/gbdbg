EXTRA_DIST =

COMPILE_TARGET = console
ASSEMBLY_COMPILER = CSC
CSC_ARGS = $(CSC_NO_UNSAFE) $(CSC_STRONGNAME)

all: $(ASSEMBLY) $(LINUX_PKGCONFIG) $(ASSEMBLY_WRAPPER)

FILES = \
	AsmFormatException.cs \
	InvalidResponseException.cs \
	LabelNotFoundException.cs \
	LabelTooFarException.cs \
	Sm83Assembler.cs \
	Sm83Assembler.Addition.cs \
	Sm83Assembler.Argument.cs \
	Sm83Assembler.DataDirective.cs \
	Sm83Assembler.Indirection.cs \
	Sm83Assembler.Instruction.cs \
	Sm83Assembler.Label.cs \
	Sm83Assembler.Negation.cs \
	Sm83Assembler.OriginDirective.cs \
	Sm83Assembler.ParserToken.cs \
	Sm83Assembler.PostDecrement.cs \
	Sm83Assembler.PostIncrement.cs \
	Sm83Assembler.Reloc.cs \
	Sm83Assembler.RelocType.cs \
	Sm83Assembler.Substraction.cs \
	Sm83Assembler.Terminal.cs \
	Sm83Debugger.cs \
	Sm83Debugger.TargetMemoryStream.cs \
	Sm83Disassembler.cs \
	Sm83LexerBase.cs \
	Sm83LexerBase.LexerToken.cs \
	Sm83LexerBase.LexerTokenType.cs \
	Sm83Registers.cs \
	Sm83StatusFlags.cs \
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
