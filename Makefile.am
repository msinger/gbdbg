EXTRA_DIST = \
m4 \
AUTHORS \
COPYING \
INSTALL \
README \
snk/gbdbg.snk

ACLOCAL_AMFLAGS = -I m4

SUBDIRS = gbdbg

DISTCLEANFILES = \
$(builddir)/test_out.exe* \
$(builddir)/test_in.* \
$(builddir)/*.userprefs \
$(builddir)/*.usertasks

distclean-local:
	rm -r -f '$(builddir)/bin' '$(builddir)/obj' '$(builddir)/autom4te.cache' '$(builddir)/.vs'

MAINTAINERCLEANFILES = \
configure \
Makefile.in \
aclocal.m4

DISTCHECK_CONFIGURE_FLAGS =
